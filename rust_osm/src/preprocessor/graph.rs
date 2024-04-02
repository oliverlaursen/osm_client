use crate::preprocessor::edge::*;
use crate::preprocessor::preprocessor::*;
use crate::Coord;

use osmpbfreader::NodeId;
use std::collections::HashMap;
use std::collections::HashSet;
use std::fs;
use std::fs::File;
use std::io;
use std::io::BufRead;
use std::io::BufReader;
use std::io::Write;
use std::path::Path;

pub struct Graph;

impl Graph {
    pub fn find_intermediate_nodes(
        graph: &Vec<Vec<Edge>>,
        nodes_pointing_to_node: &Vec<Vec<NodeId>>,
    ) -> Vec<NodeId> {
        let mut intermediate_nodes: Vec<NodeId> = Vec::new();
        for (node_id, _) in graph.iter().enumerate() {
            let edges = graph.get(node_id).unwrap();
            let mut neighbors: HashSet<NodeId> = edges.iter().map(|edge| edge.node).collect();
            let outgoing = neighbors.clone();
            let incoming = nodes_pointing_to_node
                .get(node_id)
                .unwrap_or(&Vec::new())
                .clone();
            neighbors.extend(incoming.iter());

            if neighbors.len() == 2 && incoming.len() == outgoing.len() {
                intermediate_nodes.push(NodeId(node_id.try_into().unwrap()));
            }
        }
        intermediate_nodes
    }

    pub fn find_intermediate_nodes_interwrites(
        graph: &Vec<Vec<Edge>>,
        nodes_pointing_to_node_file: &str,
    ) -> Vec<NodeId> {
        let file = std::fs::File::open(nodes_pointing_to_node_file).unwrap();
        let mut reader = std::io::BufReader::new(file);
        let mut intermediate_nodes: Vec<NodeId> = Vec::new();
        let mut buf = String::new();
        for (node_id, _) in graph.iter().enumerate() {
            let edges = graph.get(node_id).unwrap();
            let mut neighbors: HashSet<NodeId> = edges.iter().map(|edge| edge.node).collect();
            let outgoing = neighbors.clone();
            let num_bytes = reader.read_line(&mut buf).unwrap();
            let incoming:Vec<NodeId> = buf.split(' ').map(|n| NodeId(n.parse::<i64>().unwrap())).collect();
            neighbors.extend(incoming.iter());

            if neighbors.len() == 2 && incoming.len() == outgoing.len() {
                intermediate_nodes.push(NodeId(node_id as i64));
            }
        }
        intermediate_nodes
    }

    pub fn find_nodes_pointing_to_node(
        graph: &Vec<Vec<Edge>>,
    ) -> Vec<Vec<NodeId>> {
        let mut nodes_pointing_to_node: Vec<Vec<NodeId>> = Vec::with_capacity(graph.len());
        for _ in 0..graph.len() {
            nodes_pointing_to_node.push(Vec::new());
        }
        for (node_id, edges) in graph.iter().enumerate() {
            for edge in edges {
                let node = edge.node.0 as usize;
                nodes_pointing_to_node.get_mut(node_id).unwrap().push(NodeId(node as i64));
            }
        }
        nodes_pointing_to_node
    }

    pub fn fix_intermediate_nodes(
        graph: &mut Vec<Vec<Edge>>,
        nodes_pointing_to_node: &mut Vec<Vec<NodeId>>,
        intermediate_nodes: Vec<NodeId>,
    ) {
        for node_id in &intermediate_nodes {
            let edges:Vec<Edge> = graph.get(node_id.0 as usize).unwrap().clone();
            let node_id = *node_id;
            let outgoing: HashSet<NodeId> = edges.iter().map(|edge| edge.node).collect();
            let two_way = outgoing.len() == 2;
            if !two_way {
                let pred = nodes_pointing_to_node.get(node_id.0 as usize).unwrap()[0];
                print!("pred: {:?}", pred);
                let succ = edges[0].node;
                let cost = edges[0].cost
                    + graph
                        .get(pred.0 as usize)
                        .unwrap()
                        .iter()
                        .find(|x| x.node.0 == node_id.0)
                        .unwrap()
                        .cost;
                let new_edge = Edge::new(succ, cost);
                Graph::update_edges_and_remove_node(pred.0 as usize, node_id.0 as usize, graph, new_edge);
                Graph::update_nodes_pointing_to_node_edge(
                    &succ,
                    nodes_pointing_to_node,
                    &pred,
                    node_id,
                )
            } else {
                let succ = edges[0].node;
                let pred = edges.get(1).map(|x| x.node).unwrap();
                let edge_from_pred = graph.get(pred.0 as usize).unwrap().iter().find(|x| x.node == node_id);
                let cost = edges[0].cost + edge_from_pred.or(edges.get(1)).unwrap().cost;
                let new_edge_from_pred = Edge::new(succ, cost);
                let new_edge_from_succ = Edge::new(pred, cost);
                Graph::update_edges_and_remove_node(pred.0 as usize, node_id.0 as usize, graph, new_edge_from_pred);
                Graph::update_edges_and_remove_node(succ.0 as usize, node_id.0 as usize, graph, new_edge_from_succ);
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
        for (node, edges) in graph.iter_mut().enumerate() {
            edges.retain(|x| x.node.0 as usize != node);
            edges.sort_by(|a, b| a.node.0.cmp(&b.node.0));
            edges.dedup_by(|a, b| a.node == b.node);
        }
    }

    pub fn find_end_nodes(
        graph: &Vec<Vec<Edge>>,
        nodes_pointing_to_node: &Vec<Vec<NodeId>>,
    ) -> (Vec<usize>, Vec<usize>, Vec<usize>, Vec<usize>) {
        let mut end_nodes: Vec<usize> = Vec::new();
        let mut start_nodes: Vec<usize> = Vec::new();
        let mut two_way_end_nodes: Vec<usize> = Vec::new();
        let mut dead_nodes: Vec<usize> = Vec::new();
        for (node, edges) in graph.iter().enumerate() {
            let pointing = &nodes_pointing_to_node
                .get(node)
                .unwrap_or(&Vec::new())
                .clone();
            if edges.len() == 1 {
                if pointing.len() == 0 {
                    start_nodes.push(node);
                } else if pointing.len() == 1
                    && nodes_pointing_to_node.get(node).unwrap()[0] == edges[0].node
                {
                    two_way_end_nodes.push(node);
                }
            } else if pointing.len() == 0 {
                if edges.len() == 0 {
                    dead_nodes.push(node);
                } else {
                    end_nodes.push(node);
                }
            }
        }
        (end_nodes, start_nodes, two_way_end_nodes, dead_nodes)
    }

    pub fn fix_end_nodes(
        graph: &mut Vec<Vec<Edge>>,
        nodes_pointing_to_node: &mut Vec<Vec<NodeId>>,
        start_nodes: &Vec<usize>,
        end_nodes: &Vec<usize>,
        two_way_end_nodes: &Vec<usize>,
        dead_nodes: &Vec<usize>,
    ) {
        for node in start_nodes {
            let edges = graph.get_mut(*node).unwrap();
            println!("{:?}", edges);
            println!("{:?}", nodes_pointing_to_node);
            nodes_pointing_to_node
                .get_mut(edges[0].node.0 as usize)
                .unwrap()
                .clear();
            graph[*node] = Vec::new();
        }
        for node in end_nodes {
            let pred_nodes = nodes_pointing_to_node.get(*node);
            if let Some(p) = pred_nodes {
                let pred = p[0];
                let edges = graph.get_mut(pred.0 as usize).unwrap();
                edges.retain(|x| x.node != NodeId(*node as i64));
            }
            graph[*node] = Vec::new();
        }

        for node in two_way_end_nodes {
            let pred_edges = nodes_pointing_to_node.get(*node).unwrap();
            if !pred_edges.is_empty() {
                let edges = graph.get_mut(pred_edges[0].0 as usize);
                if let Some(edges) = edges {
                    nodes_pointing_to_node
                        .get_mut(edges[0].node.0 as usize)
                        .unwrap()
                        .retain(|x| x.0 as usize != *node);
                    edges.retain(|x| x.node != NodeId(*node as i64));
                }
            }
            graph[*node] = Vec::new();
        }
        for node in dead_nodes {
            graph[*node] = Vec::new();
        }
    }

    pub fn minimize_graph(graph: &mut Vec<Vec<Edge>>, remove_ends: bool) {
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
            fn can_remove_ends(
                end_nodes: &Vec<usize>,
                start_nodes: &Vec<usize>,
                two_way_end_nodes: &Vec<usize>,
                dead_nodes: &Vec<usize>,
            ) -> bool {
                !end_nodes.is_empty()
                    || !start_nodes.is_empty()
                    || !two_way_end_nodes.is_empty()
                    || !dead_nodes.is_empty()
            }
            while can_remove_ends(&end_nodes, &start_nodes, &two_way_end_nodes, &dead_nodes)
                || !intermediate_nodes.is_empty()
            {
                while can_remove_ends(&end_nodes, &start_nodes, &two_way_end_nodes, &dead_nodes) {
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
        pred: usize,
        node: usize,
        graph:&mut Vec<Vec<Edge>>,
        new_edge: Edge,
    ) {
        let mut pred_edges = graph
            .get_mut(pred)
            .unwrap_or_else(|| panic!("Could not get edges from {:?}", &pred))
            .clone();
        pred_edges.retain(|x| x.node.0 as usize != node);
        if pred_edges.iter().any(|x| x.node == new_edge.node) {
            let edge = pred_edges
                .iter_mut()
                .find(|x| x.node == new_edge.node)
                .unwrap();
            edge.cost = std::cmp::min(edge.cost, new_edge.cost);
        } else {
            pred_edges.push(new_edge);
        }
        graph[node] = Vec::new();
        graph.insert(pred, pred_edges);
    }

    fn update_nodes_pointing_to_node_edge(
        from: &NodeId,
        nodes_pointing_to_node: &mut Vec<Vec<NodeId>>,
        to: &NodeId,
        intermediate: NodeId,
    ) {
        let mut edges = nodes_pointing_to_node.get_mut(from.0 as usize).unwrap().clone();
        edges.retain(|x| *x != intermediate && *x != *to);
        edges.push(*to);
        nodes_pointing_to_node.insert(from.0 as usize, edges);
    }
    


    fn change_nth_line(filename: &str, n: usize, new_line: &str) -> io::Result<()> {
        let path = Path::new(filename);
        let file = File::open(&path)?;
        let reader = BufReader::new(file);
    
        let temp_path = Path::new("temp.txt");
        let mut temp_file = File::create(&temp_path)?;
    
        for (index, line) in reader.lines().enumerate() {
            let line = line?;
            if index == n {
                writeln!(temp_file, "{}", new_line)?;
            } else {
                writeln!(temp_file, "{}", line)?;
            }
        }
    
        fs::rename(temp_path, path)?;
    
        Ok(())
    }

    pub fn build_graph_interwrites(filename: &str) -> Vec<Vec<Edge>> {
        let mut graph: HashMap<NodeId, Vec<Edge>> = HashMap::new();
        // Read line by line and build the graph
        let file = std::fs::File::open(filename).unwrap();
        let reader = std::io::BufReader::new(file);
        for line in reader.lines() {
            let line = line.unwrap();
            let mut iter = line.split_whitespace();
            let from = NodeId(iter.next().unwrap().parse().unwrap());
            let to = NodeId(iter.next().unwrap().parse().unwrap());
            let cost = iter.next().unwrap().parse().unwrap();
            let edge_from = Edge::new(to, cost);
            let direction: u8 = iter.next().unwrap().parse().unwrap();
            graph.entry(to).or_insert(Vec::new());
            graph.entry(from).or_insert(Vec::new()).push(edge_from);
            if direction == 1 {
                let edge_to = Edge::new(from, cost);
                graph.entry(to).or_insert(Vec::new()).push(edge_to);
            }
        }
        // Remove duplicate edges
        for (_, edges) in graph.iter_mut() {
            edges.sort_by(|a, b| a.node.0.cmp(&b.node.0));
            edges.dedup_by(|a, b| a.node == b.node);
        }
        // Convert the HashMap into a Vec of tuples and sort it by NodeId
        let mut graph_vec: Vec<(NodeId, Vec<Edge>)> = graph.into_iter().collect();
        graph_vec.sort_by(|a, b| a.0.cmp(&b.0));

        // Map the sorted Vec to a Vec<Vec<Edge>>
        let graph_vec: Vec<Vec<Edge>> = graph_vec.into_iter().map(|(_, edges)| edges).collect();

        graph_vec
    }

    pub fn build_graph(
        nodes: &HashMap<NodeId, Coord>,
        roads: &Vec<Road>,
    ) -> Vec<Vec<Edge>> {
        let mut graph: HashMap<NodeId, Vec<Edge>> = HashMap::new();
        for node in nodes.keys() {
            graph.insert(*node, Vec::new());
        }
        for road in roads {
            for win in road.node_refs.windows(2) {
                let node = win[0];
                let next_node = win[1];
                let distance = nodes[&node].distance_to(nodes[&next_node]) as u32;
                let edge = Edge::new(next_node, distance);
                graph.get_mut(&node).unwrap().push(edge);
                if road.direction == CarDirection::Twoway {
                    let edge = Edge::new(node, distance);
                    graph.get_mut(&next_node).unwrap().push(edge);
                }
            }
        }
        // Remove duplicate edges
        for (_, edges) in graph.iter_mut() {
            edges.sort_by(|a, b| a.node.0.cmp(&b.node.0));
            edges.dedup_by(|a, b| a.node == b.node);
        }
        // Convert the HashMap into a Vec of tuples and sort it by NodeId
        let mut graph_vec: Vec<(NodeId, Vec<Edge>)> = graph.into_iter().collect();
        graph_vec.sort_by(|a, b| a.0.cmp(&b.0));
        let graph_vec: Vec<Vec<Edge>> = graph_vec.into_iter().map(|(_, edges)| edges).collect();

        graph_vec
    }
}

// TESTS
fn initialize(filename: &str) -> Preprocessor {
    let mut preprocessor = Preprocessor::new();
    let (mut nodes, roads) = Preprocessor::get_roads_and_nodes(filename);
    preprocessor
}

#[test]
fn can_build_full_graph() {
    // builds a graph with two nodes and one edge
    // should minimize to 0
    let mut preprocessor = initialize("src/test_data/minimal_twoway.osm.testpbf");
    let graph = preprocessor.build_graph();
    assert_eq!(graph.len(), 0);
    assert_eq!(preprocessor.nodes.len(), 2);
}

#[test]
fn can_minimize_graph() {
    // //removes one intermediate node
    // and all ends
    let mut preprocessor = initialize("src/test_data/minimize_correctly.osm.testpbf");
    let graph = preprocessor.build_graph();
    println!("{:?}", graph);
    assert_eq!(graph.len(), 0);
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
