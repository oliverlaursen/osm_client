use crate::preprocessor::edge::*;
use crate::preprocessor::preprocessor::*;

use osmpbfreader::NodeId;
use std::collections::HashMap;
use std::collections::HashSet;

pub struct Graph;

impl Graph {
    pub fn find_intermediate_nodes(
        graph: &HashMap<NodeId, Vec<Edge>>,
        nodes_pointing_to_node: &HashMap<NodeId, Vec<NodeId>>,
    ) -> Vec<NodeId> {
        let mut intermediate_nodes: Vec<NodeId> = Vec::new();
        for (node_id, _) in graph.iter() {
            let edges = graph.get(node_id).unwrap();
            let mut neighbors: HashSet<NodeId> = edges.iter().map(|edge| edge.node).collect();
            let outgoing = neighbors.clone();
            let incoming = nodes_pointing_to_node
                .get(node_id)
                .unwrap_or(&Vec::new())
                .clone();
            neighbors.extend(incoming.iter());

            if neighbors.len() == 2 && incoming.len() == outgoing.len() {
                intermediate_nodes.push(*node_id);
            }
        }
        intermediate_nodes
    }

    pub fn find_nodes_pointing_to_node(
        graph: &HashMap<NodeId, Vec<Edge>>,
    ) -> HashMap<NodeId, Vec<NodeId>> {
        let mut nodes_pointing_to_node: HashMap<NodeId, Vec<NodeId>> = HashMap::new();
        graph.iter().for_each(|(node_id, edges)| {
            edges.iter().for_each(|edge| {
                nodes_pointing_to_node
                    .entry(edge.node)
                    .or_insert_with(Vec::new)
                    .push(*node_id);
            });
        });
        nodes_pointing_to_node
    }

    pub fn fix_intermediate_nodes(
        graph: &mut HashMap<NodeId, Vec<Edge>>,
        nodes_pointing_to_node: &mut HashMap<NodeId, Vec<NodeId>>,
        intermediate_nodes: Vec<NodeId>,
    ) {
        for node_id in &intermediate_nodes {
            let edges = graph.get(node_id).unwrap();
            let node_id = *node_id;
            let outgoing: HashSet<NodeId> = edges.iter().map(|edge| edge.node).collect();
            let two_way = outgoing.len() == 2;
            if !two_way {
                let pred = nodes_pointing_to_node.get(&node_id).unwrap()[0];
                let succ = edges[0].node;
                let cost = edges[0].cost
                    + graph
                        .get(&pred)
                        .unwrap()
                        .iter()
                        .find(|x| x.node == node_id)
                        .unwrap()
                        .cost;
                let new_edge = Edge::new(succ, cost);
                Graph::update_edges_and_remove_node(pred, node_id, graph, new_edge);
                Graph::update_nodes_pointing_to_node_edge(
                    &succ,
                    nodes_pointing_to_node,
                    &pred,
                    node_id,
                )
            } else {
                let succ = edges[0].node;
                let pred = edges[1].node;
                let edge_from_pred = graph.get(&pred).unwrap().iter().find(|x| x.node == node_id);
                let cost = edges[0].cost + edge_from_pred.unwrap().cost;
                let new_edge_from_pred = Edge::new(succ, cost);
                let new_edge_from_succ = Edge::new(pred, cost);
                Graph::update_edges_and_remove_node(pred, node_id, graph, new_edge_from_pred);
                Graph::update_edges_and_remove_node(succ, node_id, graph, new_edge_from_succ);
                Graph::update_nodes_pointing_to_node_edge(
                    &pred,
                    nodes_pointing_to_node,
                    &succ,
                    node_id,
                );
                Graph::update_nodes_pointing_to_node_edge(
                    &succ,
                    nodes_pointing_to_node,
                    &pred,
                    node_id,
                );
            }
        }
        // Remove loops and duplicate edges
        for (node, edges) in graph.iter_mut() {
            edges.retain(|x| x.node != *node);
            edges.sort_by(|a, b| a.node.0.cmp(&b.node.0));
            edges.dedup_by(|a, b| a.node == b.node);
        }
    }

    pub fn find_end_nodes(
        graph: &HashMap<NodeId, Vec<Edge>>,
        nodes_pointing_to_node: &HashMap<NodeId, Vec<NodeId>>,
    ) -> (Vec<NodeId>, Vec<NodeId>, Vec<NodeId>, Vec<NodeId>) {
        let mut end_nodes: Vec<NodeId> = Vec::new();
        let mut start_nodes: Vec<NodeId> = Vec::new();
        let mut two_way_end_nodes: Vec<NodeId> = Vec::new();
        let mut dead_nodes: Vec<NodeId> = Vec::new();
        for (node, edges) in graph.iter() {
            let pointing = &nodes_pointing_to_node
                .get(node)
                .unwrap_or(&Vec::new())
                .clone();
            if edges.len() == 1 {
                if pointing.len() == 0 {
                    start_nodes.push(*node);
                } else if pointing.len() == 1
                    && nodes_pointing_to_node.get(node).unwrap()[0] == edges[0].node
                {
                    two_way_end_nodes.push(*node);
                }
            } else if pointing.len() == 0 {
                if edges.len() == 0 {
                    dead_nodes.push(*node);
                } else {
                    end_nodes.push(*node);
                }
            }
        }
        (end_nodes, start_nodes, two_way_end_nodes, dead_nodes)
    }

    pub fn fix_end_nodes(
        graph: &mut HashMap<NodeId, Vec<Edge>>,
        nodes_pointing_to_node: &mut HashMap<NodeId, Vec<NodeId>>,
        start_nodes: &Vec<NodeId>,
        end_nodes: &Vec<NodeId>,
        two_way_end_nodes: &Vec<NodeId>,
        dead_nodes: &Vec<NodeId>,
    ) {
        for node in start_nodes {
            let edges = graph.get_mut(&node).unwrap();
            nodes_pointing_to_node
                .get_mut(&edges[0].node)
                .unwrap()
                .clear();
            graph.remove(&node);
        }
        for node in end_nodes {
            let pred_nodes = nodes_pointing_to_node.get(&node);
            if let Some(p) = pred_nodes {
                let pred = p[0];
                let edges = graph.get_mut(&pred).unwrap();
                edges.retain(|x| x.node != *node);
            }
            graph.remove(&node);
        }

        for node in two_way_end_nodes {
            let pred_edges = nodes_pointing_to_node.get(&node).unwrap();
            if pred_edges.len() == 0 {
                graph.remove(&node);
            } else {
                let edges = graph.get_mut(&pred_edges[0]);
                match edges {
                    Some(edges) => {
                        nodes_pointing_to_node
                            .get_mut(&edges[0].node)
                            .unwrap()
                            .retain(|x| *x != *node);
                        edges.retain(|x| x.node != *node);
                    }
                    None => {}
                }
                graph.remove(&node);
            }
        }
        for node in dead_nodes {
            graph.remove(&node);
        }
    }

    pub fn minimize_graph(graph: &mut HashMap<NodeId, Vec<Edge>>, remove_ends: bool) {
        let mut nodes_pointing_to_node = Self::find_nodes_pointing_to_node(graph);
        let mut intermediate_nodes = Self::find_intermediate_nodes(graph, &nodes_pointing_to_node);

        while !intermediate_nodes.is_empty() {
            Self::fix_intermediate_nodes(
                graph,
                &mut nodes_pointing_to_node,
                intermediate_nodes.clone(),
            );
            nodes_pointing_to_node = Self::find_nodes_pointing_to_node(graph);
            intermediate_nodes = Self::find_intermediate_nodes(graph, &nodes_pointing_to_node);
        }
        if remove_ends {
            nodes_pointing_to_node = Self::find_nodes_pointing_to_node(graph);
            let (mut end_nodes, mut start_nodes, mut two_way_end_nodes, mut dead_nodes) =
                Self::find_end_nodes(graph, &nodes_pointing_to_node);
            fn can_remove_ends(end_nodes: &Vec<NodeId>, start_nodes: &Vec<NodeId>,two_way_end_nodes: &Vec<NodeId>,dead_nodes: &Vec<NodeId>) -> bool {
                !end_nodes.is_empty()
                || !start_nodes.is_empty()
                || !two_way_end_nodes.is_empty()
                || !dead_nodes.is_empty()
            }
            while can_remove_ends(&end_nodes,&start_nodes,&two_way_end_nodes,&dead_nodes) || !intermediate_nodes.is_empty() {
                while can_remove_ends(&end_nodes,&start_nodes,&two_way_end_nodes,&dead_nodes) {
                    Self::fix_end_nodes(
                        graph,
                        &mut nodes_pointing_to_node,
                        &start_nodes,
                        &end_nodes,
                        &two_way_end_nodes,
                        &dead_nodes,
                    );
                    nodes_pointing_to_node = Self::find_nodes_pointing_to_node(graph);
                    (end_nodes, start_nodes, two_way_end_nodes, dead_nodes) =
                        Self::find_end_nodes(graph, &nodes_pointing_to_node);
                }
                intermediate_nodes = Self::find_intermediate_nodes(graph, &nodes_pointing_to_node);
                while !intermediate_nodes.is_empty() {
                    Self::fix_intermediate_nodes(
                        graph,
                        &mut nodes_pointing_to_node,
                        intermediate_nodes.clone(),
                    );
                    nodes_pointing_to_node = Self::find_nodes_pointing_to_node(graph);
                    intermediate_nodes =
                        Self::find_intermediate_nodes(graph, &nodes_pointing_to_node);
                }
                (end_nodes, start_nodes, two_way_end_nodes, dead_nodes) =
                        Self::find_end_nodes(graph, &nodes_pointing_to_node);
            }
        }
    }

    fn update_edges_and_remove_node(
        pred: NodeId,
        node: NodeId,
        graph: &mut HashMap<NodeId, Vec<Edge>>,
        new_edge: Edge,
    ) {
        let mut pred_edges = graph
            .get_mut(&pred)
            .unwrap_or_else(|| panic!("Could not get edges from {:?}", &pred))
            .clone();
        pred_edges.retain(|x| x.node != node);
        if pred_edges.iter().any(|x| x.node == new_edge.node) {
            let edge = pred_edges
                .iter_mut()
                .find(|x| x.node == new_edge.node)
                .unwrap();
            edge.cost = std::cmp::min(edge.cost, new_edge.cost);
        } else {
            pred_edges.push(new_edge);
        }
        graph.remove(&node);
        graph.insert(pred, pred_edges);
    }

    fn update_nodes_pointing_to_node_edge(
        from: &NodeId,
        nodes_pointing_to_node: &mut HashMap<NodeId, Vec<NodeId>>,
        to: &NodeId,
        intermediate: NodeId,
    ) {
        let mut edges = nodes_pointing_to_node.get_mut(from).unwrap().clone();
        edges.retain(|x| *x != intermediate && *x != *to);
        edges.push(*to);
        nodes_pointing_to_node.insert(*from, edges);
    }

    pub fn build_graph(
        nodes: &HashMap<NodeId, Node>,
        roads: &Vec<Road>,
    ) -> HashMap<NodeId, Vec<Edge>> {
        let mut graph: HashMap<NodeId, Vec<Edge>> = HashMap::new();
        for node in nodes.values() {
            graph.insert(node.id, Vec::new());
        }
        for road in roads {
            for n_i in 0..&road.node_refs.len() - 1 {
                let node = road.node_refs[n_i];
                let next_node = road.node_refs[n_i + 1];
                let distance = nodes[&node].coord.distance_to(nodes[&next_node].coord) as u32;
                let edge = Edge::new(next_node, distance);
                graph.get_mut(&node).unwrap().push(edge);
                if road.direction == CarDirection::Twoway {
                    let edge = Edge::new(node, distance);
                    graph.get_mut(&next_node).unwrap().push(edge);
                }
            }
        }

        graph
    }
}

// TESTS
fn initialize(filename: &str) -> Preprocessor {
    let mut preprocessor = Preprocessor::new();
    preprocessor.get_roads_and_nodes(filename);
    preprocessor.filter_nodes();
    preprocessor
}

#[test]
fn can_build_full_graph() {
    // builds a graph with two nodes and one edge
    let mut preprocessor = initialize("src/test_data/minimal_twoway.osm.testpbf");
    let graph = preprocessor.build_graph();
    assert_eq!(graph.len(), 2);
    assert_eq!(preprocessor.nodes.len(), 2);
}

#[test]
fn can_minimize_graph() {
    // //removes one intermediate node
    let mut preprocessor = initialize("src/test_data/minimize_correctly.osm.testpbf");
    let graph = preprocessor.build_graph();
    println!("{:?}", graph);
    assert_eq!(graph.len(), 2);
}

#[test]
fn one_way_roads_minimization() {
    let mut graph: HashMap<NodeId, Vec<Edge>> = HashMap::new();
    graph.insert(NodeId(1), vec![Edge::new(NodeId(2), 1)]);
    graph.insert(NodeId(2), vec![Edge::new(NodeId(3), 1)]);
    let mut node_ids = Vec::new();
    for node in graph.keys() {
        node_ids.push(*node);
    }
    Graph::minimize_graph(&mut graph, false);
    assert_eq!(graph.len(), 1);
    assert_eq!(graph.get(&NodeId(1)).unwrap()[0].node, NodeId(3));
    assert_eq!(graph.get(&NodeId(1)).unwrap()[0].cost, 2);
}

#[test]
fn one_way_roads_minimization_long() {
    let mut graph: HashMap<NodeId, Vec<Edge>> = HashMap::new();
    graph.insert(NodeId(1), vec![Edge::new(NodeId(2), 1)]);
    graph.insert(NodeId(2), vec![Edge::new(NodeId(3), 1)]);
    graph.insert(NodeId(3), vec![Edge::new(NodeId(4), 1)]);
    graph.insert(NodeId(4), vec![Edge::new(NodeId(5), 1)]);
    let mut node_ids = Vec::new();
    for node in graph.keys() {
        node_ids.push(*node);
    }
    Graph::minimize_graph(&mut graph, false);
    assert_eq!(graph.len(), 1);
    assert_eq!(graph.get(&NodeId(1)).unwrap()[0].node, NodeId(5));
    assert_eq!(graph.get(&NodeId(1)).unwrap()[0].cost, 4);
}

#[test]
fn one_way_roads_with_cross() {
    let mut graph: HashMap<NodeId, Vec<Edge>> = HashMap::new();
    graph.insert(NodeId(1), vec![Edge::new(NodeId(2), 1)]);
    graph.insert(NodeId(2), vec![Edge::new(NodeId(3), 1)]);
    graph.insert(NodeId(3), vec![Edge::new(NodeId(4), 1)]);
    graph.insert(NodeId(4), vec![Edge::new(NodeId(5), 1)]);
    graph.insert(NodeId(5), Vec::new());
    graph.insert(NodeId(6), vec![Edge::new(NodeId(4), 1)]);
    graph.insert(NodeId(7), vec![Edge::new(NodeId(6), 1)]);

    let mut node_ids = Vec::new();
    for node in graph.keys() {
        node_ids.push(*node);
    }

    Graph::minimize_graph(&mut graph, false);
    assert_eq!(graph.len(), 4);
}

#[test]
fn two_way_roads_simple() {
    let mut graph: HashMap<NodeId, Vec<Edge>> = HashMap::new();
    graph.insert(NodeId(1), vec![Edge::new(NodeId(2), 1)]);
    graph.insert(
        NodeId(2),
        vec![Edge::new(NodeId(1), 1), Edge::new(NodeId(3), 1)],
    );
    graph.insert(NodeId(3), vec![Edge::new(NodeId(2), 1)]);

    let mut node_ids = Vec::new();
    for node in graph.keys() {
        node_ids.push(*node);
    }
    Graph::minimize_graph(&mut graph, false);
    assert_eq!(graph.len(), 2);
    assert_eq!(graph.get(&NodeId(1)).unwrap()[0].node, NodeId(3));
    assert_eq!(graph.get(&NodeId(1)).unwrap()[0].cost, 2);
    assert_eq!(graph.get(&NodeId(3)).unwrap()[0].node, NodeId(1));
    assert_eq!(graph.get(&NodeId(3)).unwrap()[0].cost, 2);
}

#[test]
fn one_way_cycle() {
    let mut graph: HashMap<NodeId, Vec<Edge>> = HashMap::new();
    let _node_ids = vec![NodeId(1), NodeId(2), NodeId(3), NodeId(4), NodeId(5)];
    graph.insert(NodeId(1), vec![Edge::new(NodeId(2), 1)]);
    graph.insert(
        NodeId(2),
        vec![Edge::new(NodeId(1), 1), Edge::new(NodeId(3), 1)],
    );
    graph.insert(NodeId(3), vec![Edge::new(NodeId(4), 1)]);
    graph.insert(NodeId(4), vec![Edge::new(NodeId(5), 1)]);
    graph.insert(NodeId(5), vec![Edge::new(NodeId(2), 1)]);
    Graph::minimize_graph(&mut graph, false);
    println!("{:?}", graph);
}

#[test]
fn advanced_one_way_cycle() {
    let mut graph: HashMap<NodeId, Vec<Edge>> = HashMap::new();
    graph.insert(
        NodeId(1),
        vec![Edge::new(NodeId(2), 1), Edge::new(NodeId(10), 1)],
    );
    graph.insert(NodeId(2), vec![Edge::new(NodeId(3), 1)]);
    graph.insert(NodeId(3), vec![Edge::new(NodeId(4), 1)]);
    graph.insert(NodeId(4), vec![Edge::new(NodeId(5), 1)]);
    graph.insert(
        NodeId(5),
        vec![Edge::new(NodeId(6), 1), Edge::new(NodeId(11), 1)],
    );
    graph.insert(NodeId(6), vec![Edge::new(NodeId(7), 1)]);
    graph.insert(NodeId(7), vec![Edge::new(NodeId(8), 1)]);
    graph.insert(NodeId(8), vec![Edge::new(NodeId(9), 1)]);
    graph.insert(NodeId(9), vec![Edge::new(NodeId(1), 1)]);
    graph.insert(NodeId(10), Vec::new());
    graph.insert(NodeId(11), Vec::new());
    Graph::minimize_graph(&mut graph, false);
    println!("{:?}", graph);
}

#[test]
fn two_time_minimize() {
    let mut graph: HashMap<NodeId, Vec<Edge>> = HashMap::new();
    graph.insert(NodeId(1), vec![Edge::new(NodeId(2), 1)]);
    graph.insert(
        NodeId(2),
        vec![Edge::new(NodeId(3), 1), Edge::new(NodeId(6), 1)],
    );
    graph.insert(NodeId(3), vec![Edge::new(NodeId(4), 1)]);
    graph.insert(NodeId(4), vec![Edge::new(NodeId(5), 1)]);
    graph.insert(NodeId(5), vec![Edge::new(NodeId(2), 1)]);
    Graph::minimize_graph(&mut graph, false);
    assert_eq!(graph.len(), 1);
    assert!(graph.get(&NodeId(1)).unwrap()[0] == Edge::new(NodeId(6), 2));
}

#[test]
fn remove_ends() {
    let mut graph: HashMap<NodeId, Vec<Edge>> = HashMap::new();
    graph.insert(NodeId(1), vec![Edge::new(NodeId(2), 1)]);
    graph.insert(NodeId(2), vec![Edge::new(NodeId(3), 1)]);
    graph.insert(NodeId(3), vec![Edge::new(NodeId(4), 1)]);
    graph.insert(NodeId(4), vec![Edge::new(NodeId(5), 1)]);
    graph.insert(NodeId(5), Vec::new());
    Graph::minimize_graph(&mut graph, true);
    assert_eq!(graph.len(), 0);
}

#[test]
fn two_way_cycle() {
    let mut graph: HashMap<NodeId, Vec<Edge>> = HashMap::new();
    graph.insert(NodeId(1), vec![Edge::new(NodeId(2), 1)]);
    graph.insert(
        NodeId(2),
        vec![
            Edge::new(NodeId(1), 1),
            Edge::new(NodeId(3), 1),
            Edge::new(NodeId(4), 1),
        ],
    );
    graph.insert(
        NodeId(3),
        vec![Edge::new(NodeId(2), 1), Edge::new(NodeId(4), 1)],
    );
    graph.insert(
        NodeId(4),
        vec![Edge::new(NodeId(3), 1), Edge::new(NodeId(2), 1)],
    );
    Graph::minimize_graph(&mut graph, false);
    assert_eq!(graph.len(), 2);
    println!("{:?}", graph);
}

#[test]
fn problematic_two_way_cycle() {
    let mut graph: HashMap<NodeId, Vec<Edge>> = HashMap::new();
    graph.insert(NodeId(0), vec![Edge::new(NodeId(1), 1)]);
    graph.insert(
        NodeId(1),
        vec![Edge::new(NodeId(2), 1), Edge::new(NodeId(0), 1)],
    );
    graph.insert(
        NodeId(2),
        vec![
            Edge::new(NodeId(1), 1),
            Edge::new(NodeId(3), 1),
            Edge::new(NodeId(7), 1),
        ],
    );
    graph.insert(
        NodeId(3),
        vec![Edge::new(NodeId(2), 1), Edge::new(NodeId(4), 1)],
    );
    graph.insert(
        NodeId(4),
        vec![
            Edge::new(NodeId(3), 1),
            Edge::new(NodeId(5), 1),
            Edge::new(NodeId(6), 1),
        ],
    );
    graph.insert(
        NodeId(5),
        vec![Edge::new(NodeId(4), 1), Edge::new(NodeId(6), 1)],
    );
    graph.insert(
        NodeId(6),
        vec![
            Edge::new(NodeId(5), 1),
            Edge::new(NodeId(4), 1),
            Edge::new(NodeId(7), 1),
        ],
    );
    graph.insert(
        NodeId(7),
        vec![Edge::new(NodeId(6), 1), Edge::new(NodeId(2), 1)],
    );
    Graph::minimize_graph(&mut graph, false);
    println!("{:?}", graph);
}
