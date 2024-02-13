use crate::preprocessor::edge;
use crate::preprocessor::edge::*;
use crate::preprocessor::preprocessor::*;

use osmpbfreader::{NodeId, WayId};
use rayon::iter::FromParallelIterator;
use rayon::iter::IntoParallelRefIterator;
use rayon::iter::ParallelIterator;
use serde::Serialize;
use std::clone;
use std::collections::HashMap;
use std::collections::HashSet;
use std::hash::Hash;
use std::ops::Index;

#[derive(Serialize)]
pub struct FullGraph {
    pub graph: HashMap<NodeId, Vec<Edge>>,
    pub nodes: HashMap<NodeId, (f32, f32)>,
}

impl FullGraph {
    pub fn new(graph: HashMap<NodeId, Vec<Edge>>, nodes: HashMap<NodeId, (f32, f32)>) -> Self {
        FullGraph { graph, nodes }
    }

    pub fn build_full_graph(preprocessor: &mut Preprocessor) -> FullGraph {
        let graph = FullGraph::graph_from_preprocessor(preprocessor);
        let graph = FullGraph::minimize_graph(graph);
        //preprocessor.remove_nodes(removed_nodes);
        let projected_points: HashMap<NodeId, (f32, f32)> = preprocessor.project_nodes_to_2d();

        FullGraph::new(graph, projected_points)
    }

    pub fn graph_from_preprocessor(preprocessor: &mut Preprocessor) -> HashMap<NodeId, Vec<Edge>> {
        let roads: HashMap<WayId, Road> = preprocessor
            .roads
            .par_iter()
            .map(|road| (road.id.clone(), road.clone()))
            .collect();

        let graph = FullGraph::build_graph(&preprocessor.nodes, &roads);
        graph
    }

    fn remove_intermediary_with_two_neighbors(
        graph: HashMap<NodeId, Vec<Edge>>,
<<<<<<< HEAD
        mut minimized_graph: HashMap<NodeId, Vec<Edge>>,
        node: &NodeId,
        neighbors: &HashSet<NodeId>,
    ) -> HashMap<NodeId, Vec<Edge>> {
        let neighbor1 = neighbors.iter().next().unwrap();
        let neighbor2 = neighbors.iter().nth(1).unwrap();
        let edges = graph.get(node).unwrap().clone();

        let mut cost = 0;
        for edge in &edges {
            cost += edge.cost;
        }

        if (neighbors.len() == 1) {
            let successor = edges[0];
            let predecessor_id = if (*neighbor1 == successor.node) {
                neighbor2
            } else {
                neighbor1
            };
            let predecessor = Edge {
                node: *predecessor_id,
                cost: cost,
            };
            FullGraph::insert_in_graph_or_update_edges(
                &mut minimized_graph,
                predecessor.node,
                predecessor,
            );
        } else {
            let new_edge1 = Edge {
                node: *neighbor1,
                cost,
            };

            let new_edge2 = Edge {
                node: *neighbor2,
                cost,
            };
            FullGraph::insert_in_graph_or_update_edges(&mut minimized_graph, *neighbor1, new_edge2);
            FullGraph::insert_in_graph_or_update_edges(&mut minimized_graph, *neighbor2, new_edge1);
        }
        FullGraph::remove_node_from_edges(&mut minimized_graph, *neighbor1, *node);
        FullGraph::remove_node_from_edges(&mut minimized_graph, *neighbor2, *node);

        minimized_graph.remove(node);

        minimized_graph
    }

    fn remove_intermediary_with_more_than_two_neighbors(
        graph: HashMap<NodeId, Vec<Edge>>,
        mut minimized_graph: HashMap<NodeId, Vec<Edge>>,
        node: &NodeId,
        outgoing: &HashSet<NodeId>,
        incoming: HashSet<NodeId>,
    ) -> HashMap<NodeId, Vec<Edge>> {
        let succ_id = outgoing.iter().next().unwrap();

        let successor = graph.get(succ_id).unwrap();
        let node_val = graph.get(node).unwrap();

        let mut succ_cost = 0;
        for edge in node_val {
            if(edge.node == *succ_id) {
                succ_cost = edge.cost;
            }
        }

        //nodes_pointing_to_node.get(&node_id).unwrap().iter().for_each(|&x| { neighbors.insert(x); });
        for id in incoming {
            let mut edges = graph.get(&id).expect("could not get node");
            for edge in edges {
                let mut new_cost = 0;
                if (edge.node == *node) {
                    new_cost = edge.cost + succ_cost;
                }
                let new_edge = Edge::new(*succ_id, new_cost);
                FullGraph::insert_in_graph_or_update_edges(&mut minimized_graph, id, new_edge);
                FullGraph::remove_node_from_edges(&mut minimized_graph, id, *node);
            }
            //let new_cost = new_edge.cost;
        }
        minimized_graph.remove(node);
        minimized_graph
    }

    fn insert_in_graph_or_update_edges(
        graph: &mut HashMap<NodeId, Vec<Edge>>,
        node: NodeId,
        edge: Edge,
    ) {
        if (node == edge.node) {
            return;
        }
        if !graph.contains_key(&node) {
            graph.insert(node, vec![edge]);
        } else {
            graph.get_mut(&node).unwrap().push(edge);
        }
    }

    fn remove_node_from_edges(
        graph: &mut HashMap<NodeId, Vec<Edge>>,
        node: NodeId,
        node_to_remove: NodeId,
    ) {
        if(graph.contains_key(&node)){
            graph.get_mut(&node).unwrap().retain(|x| x.node != node_to_remove);
        }
    }

    pub fn minimize_graph(graph: HashMap<NodeId, Vec<Edge>>) -> HashMap<NodeId, Vec<Edge>> {
=======
    ) -> (HashMap<NodeId, Vec<Edge>>, HashSet<NodeId>) {
>>>>>>> ba5cabd02d2e4393712e3dcefdbd8126fbab9acc
        let mut minimized_graph: HashMap<NodeId, Vec<Edge>> = HashMap::new();
        let mut intermediate_nodes: HashSet<NodeId> = HashSet::new();
        let mut nodes_pointing_to_node: HashMap<NodeId, Vec<NodeId>> = HashMap::new();

        // Populate nodes_pointing_to_node
        graph.iter().for_each(|(node_id, edges)| {
            edges.iter().for_each(|edge| {
                nodes_pointing_to_node
                    .entry(edge.node)
                    .or_insert_with(Vec::new)
                    .push(*node_id);
            });
        });
<<<<<<< HEAD

        // Find all intermediate nodes
        for (index, (node_id, edges)) in graph.clone().iter().enumerate() {
            let mut neighbors: HashSet<NodeId> = edges.iter().map(|edge| edge.node).collect();
            let outgoing = neighbors.clone();
            let incoming = HashSet::from_iter(
                nodes_pointing_to_node
                    .get(&node_id)
                    .unwrap_or(&Vec::new())
                    .clone(),
            );

            if neighbors.len() == 2 && incoming == outgoing {
                minimized_graph = FullGraph::remove_intermediary_with_two_neighbors(
                    graph.clone(),
                    minimized_graph.clone(),
                    &node_id,
                    &neighbors,
                );
                //intermediate_nodes.insert(*node_id);
            } else if incoming.len() >= 2 && outgoing.len() == 1 {
                minimized_graph = FullGraph::remove_intermediary_with_more_than_two_neighbors(
                    graph.clone(),
                    minimized_graph.clone(),
                    &node_id,
                    &outgoing,
                    incoming,
                );
                //nodes_pointing_to_node.get(&node_id).unwrap().iter().for_each(|&x| { neighbors.insert(x); });
            } else {
                if(!minimized_graph.contains_key(node_id)){
                    minimized_graph.insert(*node_id, edges.clone());
=======
        // Making unique neighbours collection from nodes_pointing_to_node and graph
        let mut neighbours: HashMap<NodeId, HashSet<NodeId>> = HashMap::new();
        for (node_id, edges) in &graph {
            let mut neighbours_set: HashSet<NodeId> = HashSet::new();
            for edge in edges {
                neighbours_set.insert(edge.node);
            }
            if let Some(nodes) = nodes_pointing_to_node.get(node_id) {
                for node in nodes {
                    neighbours_set.insert(*node);
                }
            }
            neighbours.insert(*node_id, neighbours_set);
        }

        fn is_end_node(neighbours: HashSet<NodeId>) -> bool {
            neighbours.len() == 1
        }
        fn is_intermediate_node(
            neighbours: HashSet<NodeId>,
            incoming: usize,
            outgoing: usize,
        ) -> bool {
            neighbours.len() == 2 && incoming == outgoing
        }

        // Identify intermediate nodes
        for (node_id, edges) in &graph {
            let incoming = nodes_pointing_to_node
                .get(node_id)
                .unwrap_or(&Vec::new())
                .len();
            let outgoing = edges.len();
            let neighbours = neighbours.get(node_id).unwrap();
            if is_intermediate_node(neighbours.clone(), incoming, outgoing) {
                intermediate_nodes.insert(*node_id);
            }
        }

        // Function to recursively find the next non-intermediate node and accumulate weight
        fn find_next_node(
            current_node: NodeId,
            graph: &HashMap<NodeId, Vec<Edge>>,
            intermediate_nodes: &HashSet<NodeId>,
            accumulated_weight: u32,
        ) -> (NodeId, u32) {
            if intermediate_nodes.contains(&current_node) {
                let edge = &graph[&current_node][0]; // Assuming single outgoing edge for intermediates
                find_next_node(
                    edge.node,
                    graph,
                    intermediate_nodes,
                    accumulated_weight + edge.cost,
                )
            } else {
                (current_node, accumulated_weight)
            }
        }

        // Adjusting the graph by skipping intermediate nodes
        for (node_id, edges) in &graph {
            println!("startnode: {:?}", node_id);
            if !intermediate_nodes.contains(node_id) {
                let mut minimized_edges: Vec<Edge> = Vec::new();
                for edge in edges {
                    if !intermediate_nodes.contains(&edge.node) {
                        minimized_edges.push(*edge);
                    } else {
                        // Use DFS to find the end node and total cost of the sequence of intermediate nodes
                        let mut stack = vec![(edge.node, edge.cost)];
                        let mut visited = HashSet::new();
                        let mut total_cost = 0;
                        let mut end_node = edge.node;
                        while let Some((node, cost)) = stack.pop() {
                            if !visited.contains(&node) {
                                visited.insert(node);
                                total_cost += cost;
                                for edge in &graph[&node] {
                                    if intermediate_nodes.contains(&edge.node) {
                                        println!("Intermediate node: {:?}", edge.node);
                                        stack.push((edge.node, edge.cost));
                                    } else {
                                        println!("End node: {:?}", edge.node);
                                        end_node = edge.node;
                                    }
                                }
                            }
                        }
                        minimized_edges.push(Edge {
                            node: end_node,
                            cost: total_cost,
                        });
                    }
>>>>>>> ba5cabd02d2e4393712e3dcefdbd8126fbab9acc
                }
                //node is not intermediate
            }
        }
        minimized_graph
    }

    pub fn build_graph(
        nodes: &HashMap<NodeId, Node>,
        roads: &HashMap<WayId, Road>,
    ) -> HashMap<NodeId, Vec<Edge>> {
        let mut graph: HashMap<NodeId, Vec<Edge>> = HashMap::new();
        let mut roads_associated_with_node: HashMap<NodeId, Vec<WayId>> = HashMap::new();
        for road in roads {
            for node in &road.1.node_refs {
                roads_associated_with_node
                    .entry(*node)
                    .or_insert(Vec::new())
                    .push(*road.0);
            }
        }
        // For each road, add the next node to the graph
        for node in roads_associated_with_node {
            let mut edges: Vec<Edge> = Vec::new();
            for road_id in node.1.iter() {
                let road = roads.get(&road_id).unwrap();
                let index = road.node_refs.iter().position(|x| *x == node.0).unwrap();
                if index != road.node_refs.len() -1 {
                    let next_node = road.node_refs[index + 1];
                    let distance = nodes[&node.0].coord.distance_to(nodes[&next_node].coord) as u32;
                    edges.push(Edge {
                        node: next_node,
                        cost: distance,
                    });
                }
                // Handle two way roads
                if index != 0 && road.direction == CarDirection::TWOWAY {
                    let next_node = road.node_refs[index - 1];
                    let distance = nodes[&node.0].coord.distance_to(nodes[&next_node].coord) as u32;
                    edges.push(Edge {
                        node: next_node,
                        cost: distance,
                    });
                }
            }
            graph.insert(node.0, edges);
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
    let graph = FullGraph::build_full_graph(&mut preprocessor);
    assert_eq!(graph.graph.len(), 2);
    assert_eq!(graph.nodes.len(), 2);
}

#[test]
fn can_minimize_graph() {
    // //removes one intermediate node
    let mut preprocessor = initialize("src/test_data/minimize_correctly.osm.testpbf");
    let graph = FullGraph::graph_from_preprocessor(&mut preprocessor);
    let (minimized_graph) = FullGraph::minimize_graph(graph);
    assert_eq!(minimized_graph.len(), 2);
}

#[test]
fn can_go_both_ways_after_minimization() {
    // checks if the graph is still two-way after minimization
    let mut preprocessor = initialize("src/test_data/minimize_correctly.osm.testpbf");
    let full_graph = FullGraph::build_full_graph(&mut preprocessor);
    assert_eq!(
        NodeId(8),
        full_graph.graph.get(&NodeId(10)).unwrap()[0].node
    ); // node 8 has an edge to node 10
    assert_eq!(
        NodeId(10),
        full_graph.graph.get(&NodeId(8)).unwrap()[0].node
    ); // node 10 has an edge to node 8
}
