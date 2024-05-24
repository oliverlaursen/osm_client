use crate::preprocessor::edge::*;
use crate::preprocessor::preprocessor::*;
use crate::Coord;

use ordered_float::OrderedFloat;
use osmpbfreader::NodeId;
use std::collections::BinaryHeap;
use std::collections::HashMap;
use std::collections::HashSet;

pub struct Graph;

impl Graph {
    pub fn get_bidirectional_graph(
        graph: &HashMap<NodeId, Vec<Edge>>,
    ) -> HashMap<NodeId, Vec<Edge>> {
        let mut bi_graph: HashMap<NodeId, Vec<Edge>> = HashMap::new();
        for (node, edges) in graph.iter() {
            for edge in edges {
                bi_graph
                    .entry(edge.node)
                    .or_insert_with(Vec::new)
                    .push(Edge::new(*node, edge.cost));
            }
        }
        bi_graph
    }

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
                let pred = edges.get(1).map(|x| x.node).unwrap();
                let edge_from_pred = graph.get(&pred).unwrap().iter().find(|x| x.node == node_id);
                let cost = edges[0].cost + edge_from_pred.or(edges.get(1)).unwrap().cost;
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
        Self::remove_duplicate_edges(graph);
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
            if !pred_edges.is_empty() {
                let edges = graph.get_mut(&pred_edges[0]);
                if let Some(edges) = edges {
                    nodes_pointing_to_node
                        .get_mut(&edges[0].node)
                        .unwrap()
                        .retain(|x| *x != *node);
                    edges.retain(|x| x.node != *node);
                }
            }
            graph.remove(&node);
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
            fn can_remove_ends(
                end_nodes: &Vec<NodeId>,
                start_nodes: &Vec<NodeId>,
                two_way_end_nodes: &Vec<NodeId>,
                dead_nodes: &Vec<NodeId>,
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
        Self::remove_duplicate_edges(graph);
    }

    fn remove_duplicate_edges(graph: &mut HashMap<NodeId, Vec<Edge>>) {
        for (node, edges) in graph.iter_mut() {
            edges.retain(|x| x.node != *node);
            edges.sort_unstable_by_key(|e| e.node.0);
            edges.dedup_by(|a, b| a.node == b.node);
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
        if let Some(edge) = pred_edges.iter_mut().find(|x| x.node == new_edge.node) {
            if let Some(cmp) = edge.cost.partial_cmp(&new_edge.cost) {
                if cmp == std::cmp::Ordering::Greater {
                    edge.cost = new_edge.cost;
                }
            }
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
        nodes: &HashMap<NodeId, Coord>,
        roads: &Vec<Road>,
    ) -> HashMap<NodeId, Vec<Edge>> {
        let mut graph: HashMap<NodeId, Vec<Edge>> = HashMap::with_capacity(nodes.len());
        let mut sorted_nodes: Vec<&NodeId> = nodes.keys().collect();
        sorted_nodes.sort();
        for node in &sorted_nodes {
            graph.insert(**node, Vec::new());
        }
        for road in roads {
            for win in road.node_refs.windows(2) {
                let node = win[0];
                let next_node = win[1];
                let distance = nodes[&node].distance_to(nodes[&next_node]);
                let edge = Edge::new(next_node, distance);
                graph.get_mut(&node).unwrap().push(edge);
                if road.direction == CarDirection::Twoway {
                    let edge = Edge::new(node, distance);
                    graph.get_mut(&next_node).unwrap().push(edge);
                }
            }
        }
        Self::remove_duplicate_edges(&mut graph);

        graph
    }

    pub fn random_landmarks(
        graph: &HashMap<NodeId, Vec<Edge>>,
        bi_graph: &HashMap<NodeId, Vec<Edge>>,
        n: u32,
    ) -> Vec<Landmark> {
        /*
           Returns n random node-ids
        */
        let mut landmarks = Vec::new();
        let mut it = graph.iter();
        for i in 0..n {
            let node = it.next().unwrap();
            let node_id = node.0.clone();
            let distances = Graph::dijkstra_all(&graph, node_id);
            let bi_distances = Graph::dijkstra_all(&bi_graph, node_id);
            landmarks.push(Landmark {
                node_id,
                distances,
                bi_distances,
            });
        }
        landmarks
    }

    pub fn farthest_landmarks(
        graph: &HashMap<NodeId, Vec<Edge>>,
        bi_graph: &HashMap<NodeId, Vec<Edge>>,
        n: u32,
    ) -> Vec<Landmark> {
        let mut landmarks = Vec::new();

        // Select an initial random node
        let mut current = *graph.keys().next().unwrap();

        for _ in 0..n {
            // Compute distances from the current node
            let distances = Graph::dijkstra_all(graph, current);
            let bi_distances = Graph::dijkstra_all(bi_graph, current);

            // Store the current landmark
            landmarks.push(Landmark {
                node_id: current,
                distances,
                bi_distances,
            });

            // Find the node farthest from all current landmarks
            let mut max_dist = 0.0;
            let mut next_node = current;

            for &node in graph.keys() {
                let min_dist_to_landmarks = landmarks
                    .iter()
                    .map(|landmark| landmark.distances.get(node.0 as usize).unwrap_or(&f32::MAX))
                    .map(|&dist| OrderedFloat(dist)) // Convert f64 to OrderedFloat<f64>
                    .min()
                    .unwrap()
                    .into_inner(); // Convert OrderedFloat<f64> back to f64

                if min_dist_to_landmarks > max_dist
                    && min_dist_to_landmarks != f32::MAX
                    && min_dist_to_landmarks != 0.0
                {
                    max_dist = min_dist_to_landmarks;
                    next_node = node;
                }
            }

            current = next_node; // Update the current node to the next landmark
        }

        landmarks
    }

    pub fn dijkstra_all(graph: &HashMap<NodeId, Vec<Edge>>, start: NodeId) -> Vec<f32> {
        // Initialize the distance map with infinite distances
        let mut distances = HashMap::new();
        for node in graph.keys() {
            distances.insert(*node, f32::MAX);
        }

        // Use a binary heap as a priority queue where the smallest distances come out first
        let mut heap = BinaryHeap::new();

        // Initialize the distance of the start node to 0 and add it to the priority queue
        distances.insert(start, 0.0);
        heap.push(Edge {
            cost: 0.0,
            node: start,
        });

        // While there are nodes still to process...
        while let Some(Edge { cost, node }) = heap.pop() {
            // If we find a shorter path to the node, skip processing
            if cost > distances[&node] {
                continue;
            }

            // Process all adjacent edges
            if let Some(edges) = graph.get(&node) {
                for edge in edges {
                    let next = Edge {
                        cost: cost + edge.cost,
                        node: edge.node,
                    };

                    // Only consider this new path if it's better
                    if next.cost < distances[&next.node] {
                        heap.push(next);
                        distances.insert(next.node, next.cost);
                    }
                }
            }
        }
        let mut distances = distances
            .iter()
            .map(|(k, v)| (*k, *v))
            .collect::<Vec<(NodeId, f32)>>();
        distances.sort_by(|a, b| a.0.cmp(&b.0));
        let distances: Vec<f32> = distances.iter().map(|x| x.1).collect();

        distances
    }
}

// TESTS
fn initialize(filename: &str) -> Preprocessor {
    let mut preprocessor = Preprocessor::new();
    preprocessor.get_roads_and_nodes(filename);
    preprocessor
}

#[test]
fn can_build_full_graph() {
    // builds a graph with two nodes and one edge
    // should minimize to 0
    let mut preprocessor = initialize("src/test_data/minimal_twoway.osm.testpbf");
    let graph = preprocessor.build_graph();
    assert_eq!(graph.0.len(), 0);
    assert_eq!(preprocessor.nodes.len(), 2);
}

#[test]
fn can_minimize_graph() {
    // //removes one intermediate node
    // and all ends
    let mut preprocessor = initialize("src/test_data/minimize_correctly.osm.testpbf");
    let graph = preprocessor.build_graph();
    assert_eq!(graph.0.len(), 0);
}

#[test]
fn one_way_roads_minimization() {
    let mut graph: HashMap<NodeId, Vec<Edge>> = HashMap::new();
    graph.insert(NodeId(1), vec![Edge::new(NodeId(2), 1.0)]);
    graph.insert(NodeId(2), vec![Edge::new(NodeId(3), 1.0)]);
    let mut node_ids = Vec::new();
    for node in graph.keys() {
        node_ids.push(*node);
    }
    Graph::minimize_graph(&mut graph, false);
    assert_eq!(graph.len(), 1);
    assert_eq!(graph.get(&NodeId(1)).unwrap()[0].node, NodeId(3));
    assert_eq!(graph.get(&NodeId(1)).unwrap()[0].cost, 2.0);
}

#[test]
fn one_way_roads_minimization_long() {
    let mut graph: HashMap<NodeId, Vec<Edge>> = HashMap::new();
    graph.insert(NodeId(1), vec![Edge::new(NodeId(2), 1.0)]);
    graph.insert(NodeId(2), vec![Edge::new(NodeId(3), 1.0)]);
    graph.insert(NodeId(3), vec![Edge::new(NodeId(4), 1.0)]);
    graph.insert(NodeId(4), vec![Edge::new(NodeId(5), 1.0)]);
    let mut node_ids = Vec::new();
    for node in graph.keys() {
        node_ids.push(*node);
    }
    Graph::minimize_graph(&mut graph, false);
    assert_eq!(graph.len(), 1);
    assert_eq!(graph.get(&NodeId(1)).unwrap()[0].node, NodeId(5));
    assert_eq!(graph.get(&NodeId(1)).unwrap()[0].cost, 4.0);
}

#[test]
fn one_way_roads_with_cross() {
    let mut graph: HashMap<NodeId, Vec<Edge>> = HashMap::new();
    graph.insert(NodeId(1), vec![Edge::new(NodeId(2), 1.0)]);
    graph.insert(NodeId(2), vec![Edge::new(NodeId(3), 1.0)]);
    graph.insert(NodeId(3), vec![Edge::new(NodeId(4), 1.0)]);
    graph.insert(NodeId(4), vec![Edge::new(NodeId(5), 1.0)]);
    graph.insert(NodeId(5), Vec::new());
    graph.insert(NodeId(6), vec![Edge::new(NodeId(4), 1.0)]);
    graph.insert(NodeId(7), vec![Edge::new(NodeId(6), 1.0)]);

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
    graph.insert(NodeId(1), vec![Edge::new(NodeId(2), 1.0)]);
    graph.insert(
        NodeId(2),
        vec![Edge::new(NodeId(1), 1.0), Edge::new(NodeId(3), 1.0)],
    );
    graph.insert(NodeId(3), vec![Edge::new(NodeId(2), 1.0)]);

    let mut node_ids = Vec::new();
    for node in graph.keys() {
        node_ids.push(*node);
    }
    Graph::minimize_graph(&mut graph, false);
    assert_eq!(graph.len(), 2);
    assert_eq!(graph.get(&NodeId(1)).unwrap()[0].node, NodeId(3));
    assert_eq!(graph.get(&NodeId(1)).unwrap()[0].cost, 2.0);
    assert_eq!(graph.get(&NodeId(3)).unwrap()[0].node, NodeId(1));
    assert_eq!(graph.get(&NodeId(3)).unwrap()[0].cost, 2.0);
}

#[test]
fn one_way_cycle() {
    let mut graph: HashMap<NodeId, Vec<Edge>> = HashMap::new();
    let _node_ids = vec![NodeId(1), NodeId(2), NodeId(3), NodeId(4), NodeId(5)];
    graph.insert(NodeId(1), vec![Edge::new(NodeId(2), 1.0)]);
    graph.insert(
        NodeId(2),
        vec![Edge::new(NodeId(1), 1.0), Edge::new(NodeId(3), 1.0)],
    );
    graph.insert(NodeId(3), vec![Edge::new(NodeId(4), 1.0)]);
    graph.insert(NodeId(4), vec![Edge::new(NodeId(5), 1.0)]);
    graph.insert(NodeId(5), vec![Edge::new(NodeId(2), 1.0)]);
    Graph::minimize_graph(&mut graph, false);
    println!("{:?}", graph);
}

#[test]
fn advanced_one_way_cycle() {
    let mut graph: HashMap<NodeId, Vec<Edge>> = HashMap::new();
    graph.insert(
        NodeId(1),
        vec![Edge::new(NodeId(2), 1.0), Edge::new(NodeId(10), 1.0)],
    );
    graph.insert(NodeId(2), vec![Edge::new(NodeId(3), 1.0)]);
    graph.insert(NodeId(3), vec![Edge::new(NodeId(4), 1.0)]);
    graph.insert(NodeId(4), vec![Edge::new(NodeId(5), 1.0)]);
    graph.insert(
        NodeId(5),
        vec![Edge::new(NodeId(6), 1.0), Edge::new(NodeId(11), 1.0)],
    );
    graph.insert(NodeId(6), vec![Edge::new(NodeId(7), 1.0)]);
    graph.insert(NodeId(7), vec![Edge::new(NodeId(8), 1.0)]);
    graph.insert(NodeId(8), vec![Edge::new(NodeId(9), 1.0)]);
    graph.insert(NodeId(9), vec![Edge::new(NodeId(1), 1.0)]);
    graph.insert(NodeId(10), Vec::new());
    graph.insert(NodeId(11), Vec::new());
    Graph::minimize_graph(&mut graph, false);
    println!("{:?}", graph);
}

#[test]
fn two_time_minimize() {
    let mut graph: HashMap<NodeId, Vec<Edge>> = HashMap::new();
    graph.insert(NodeId(1), vec![Edge::new(NodeId(2), 1.0)]);
    graph.insert(
        NodeId(2),
        vec![Edge::new(NodeId(3), 1.0), Edge::new(NodeId(6), 1.0)],
    );
    graph.insert(NodeId(3), vec![Edge::new(NodeId(4), 1.0)]);
    graph.insert(NodeId(4), vec![Edge::new(NodeId(5), 1.0)]);
    graph.insert(NodeId(5), vec![Edge::new(NodeId(2), 1.0)]);
    Graph::minimize_graph(&mut graph, false);
    assert_eq!(graph.len(), 1);
    assert!(graph.get(&NodeId(1)).unwrap()[0] == Edge::new(NodeId(6), 2.0));
}

#[test]
fn remove_ends() {
    let mut graph: HashMap<NodeId, Vec<Edge>> = HashMap::new();
    graph.insert(NodeId(1), vec![Edge::new(NodeId(2), 1.0)]);
    graph.insert(NodeId(2), vec![Edge::new(NodeId(3), 1.0)]);
    graph.insert(NodeId(3), vec![Edge::new(NodeId(4), 1.0)]);
    graph.insert(NodeId(4), vec![Edge::new(NodeId(5), 1.0)]);
    graph.insert(NodeId(5), Vec::new());
    Graph::minimize_graph(&mut graph, true);
    assert_eq!(graph.len(), 0);
}

#[test]
fn two_way_cycle() {
    let mut graph: HashMap<NodeId, Vec<Edge>> = HashMap::new();
    graph.insert(NodeId(1), vec![Edge::new(NodeId(2), 1.0)]);
    graph.insert(
        NodeId(2),
        vec![
            Edge::new(NodeId(1), 1.0),
            Edge::new(NodeId(3), 1.0),
            Edge::new(NodeId(4), 1.0),
        ],
    );
    graph.insert(
        NodeId(3),
        vec![Edge::new(NodeId(2), 1.0), Edge::new(NodeId(4), 1.0)],
    );
    graph.insert(
        NodeId(4),
        vec![Edge::new(NodeId(3), 1.0), Edge::new(NodeId(2), 1.0)],
    );
    Graph::minimize_graph(&mut graph, false);
    assert_eq!(graph.len(), 2);
    println!("{:?}", graph);
}

#[test]
fn problematic_two_way_cycle() {
    let mut graph: HashMap<NodeId, Vec<Edge>> = HashMap::new();
    graph.insert(NodeId(0), vec![Edge::new(NodeId(1), 1.0)]);
    graph.insert(
        NodeId(1),
        vec![Edge::new(NodeId(2), 1.0), Edge::new(NodeId(0), 1.0)],
    );
    graph.insert(
        NodeId(2),
        vec![
            Edge::new(NodeId(1), 1.0),
            Edge::new(NodeId(3), 1.0),
            Edge::new(NodeId(7), 1.0),
        ],
    );
    graph.insert(
        NodeId(3),
        vec![Edge::new(NodeId(2), 1.0), Edge::new(NodeId(4), 1.0)],
    );
    graph.insert(
        NodeId(4),
        vec![
            Edge::new(NodeId(3), 1.0),
            Edge::new(NodeId(5), 1.0),
            Edge::new(NodeId(6), 1.0),
        ],
    );
    graph.insert(
        NodeId(5),
        vec![Edge::new(NodeId(4), 1.0), Edge::new(NodeId(6), 1.0)],
    );
    graph.insert(
        NodeId(6),
        vec![
            Edge::new(NodeId(5), 1.0),
            Edge::new(NodeId(4), 1.0),
            Edge::new(NodeId(7), 1.0),
        ],
    );
    graph.insert(
        NodeId(7),
        vec![Edge::new(NodeId(6), 1.0), Edge::new(NodeId(2), 1.0)],
    );
    Graph::minimize_graph(&mut graph, false);
    println!("{:?}", graph);
}
