use crate::preprocessor::edge::*;
use crate::preprocessor::preprocessor::*;

use osmpbfreader::{NodeId, WayId};
use rayon::iter::FromParallelIterator;
use rayon::iter::IntoParallelRefIterator;
use rayon::iter::ParallelIterator;
use serde::Serialize;
use std::collections::HashMap;
use std::collections::HashSet;

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
        let roads: HashMap<WayId, Road> = preprocessor
            .roads
            .par_iter()
            .map(|road| (road.id.clone(), road.clone()))
            .collect();

        let graph = FullGraph::build_graph(&preprocessor.nodes, &roads);
        //let (graph, removed_nodes) = FullGraph::minimize_graph(graph);
        //preprocessor.remove_nodes(removed_nodes);
        let projected_points: HashMap<NodeId, (f32, f32)> = preprocessor.project_nodes_to_2d();

        FullGraph::new(graph, projected_points)
    }

    pub fn minimize_graph(
        graph: HashMap<NodeId, Vec<Edge>>,
    ) -> (HashMap<NodeId, Vec<Edge>>, HashSet<NodeId>) {
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
            if !intermediate_nodes.contains(node_id) {
                let mut minimized_edges: Vec<Edge> = Vec::new();
                for edge in edges {
                    if intermediate_nodes.contains(&edge.node) {
                        let (next_node, total_cost) =
                            find_next_node(edge.node, &graph, &intermediate_nodes, edge.cost);
                        if next_node != *node_id {
                            // Avoid self-loops
                            minimized_edges.push(Edge {
                                node: next_node,
                                cost: total_cost,
                            });
                        }
                    } else {
                        minimized_edges.push(*edge);
                    }
                }
                minimized_graph.insert(*node_id, minimized_edges);
            }
        }

        (minimized_graph, intermediate_nodes)
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
            for road_id in node.1 {
                let road = roads.get(&road_id).unwrap();
                let index = road.node_refs.iter().position(|x| *x == node.0).unwrap();
                if index != 0 {
                    let next_node = road.node_refs[index - 1];
                    let distance = nodes[&node.0].coord.distance_to(nodes[&next_node].coord) as u32;
                    edges.push(Edge {
                        node: next_node,
                        cost: distance,
                    });
                }
                // Handle two way roads
                if index != road.node_refs.len() - 1 && road.direction == CarDirection::TWOWAY {
                    let next_node = road.node_refs[index + 1];
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
