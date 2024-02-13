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
        let (graph, removed_nodes) = FullGraph::minimize_graph(graph);
        preprocessor.remove_nodes(removed_nodes);
        let projected_points: HashMap<NodeId, (f32, f32)> = preprocessor.project_nodes_to_2d();

        FullGraph::new(graph, projected_points)
    }

    pub fn minimize_graph(
        graph: HashMap<NodeId, Vec<Edge>>,
    ) -> (HashMap<NodeId, Vec<Edge>>, HashSet<NodeId>) {
        /*
         * Minimize the graph by removing intermediate edges
         * If a node has one edge only and only one edge points to it,
         * it is considered intermediate
         *
         * Also this incoming and outgoing edge can be two-way roads
         */
        let mut minimized_graph: HashMap<NodeId, Vec<Edge>> = HashMap::new();
        let mut intermediate_nodes: HashSet<NodeId> = HashSet::new();
        let mut nodes_pointing_to_node: HashMap<NodeId, Vec<NodeId>> = HashMap::new();
        graph.iter().for_each(|(node_id, edges)| {
            edges.iter().for_each(|edge| {
                nodes_pointing_to_node
                    .entry(edge.node)
                    .or_insert(Vec::new())
                    .push(*node_id);
            });
        });
        // Find all intermediate nodes
        for (node_id, edges) in &graph {
            let mut neighbors: HashSet<NodeId> = edges.iter().map(|edge| edge.node).collect();
            let outoing = neighbors.clone();
            let incoming = HashSet::from_iter(nodes_pointing_to_node.get(node_id).unwrap_or(&Vec::new()).clone());
            if incoming.len() > 0 {
                nodes_pointing_to_node.get(node_id).unwrap().iter().for_each(|&x| { neighbors.insert(x); });
            }
            if neighbors.len() == 2 && incoming == outoing {
                intermediate_nodes.insert(*node_id);
            }
        }

        // Build the minimized graph
        for (node_id, edges) in &graph {
            if !intermediate_nodes.contains(node_id) {
                let mut minimized_edges = Vec::new();
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
                                        stack.push((edge.node, edge.cost));
                                    } else {
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
