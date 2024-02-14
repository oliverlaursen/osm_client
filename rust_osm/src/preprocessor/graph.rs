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


    pub fn minimize_graph(graph: HashMap<NodeId, Vec<Edge>>) -> HashMap<NodeId, Vec<Edge>> {
        let mut minimized_graph: HashMap<NodeId, Vec<Edge>> = graph.clone();
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

        // Find all intermediate nodes
        for (node_id, _) in graph.clone() {
            let edges = minimized_graph.get_mut(&node_id).unwrap();
            let mut neighbors: HashSet<NodeId> = edges.iter().map(|edge| edge.node).collect();
            let outgoing = neighbors.clone();
            let incoming = 
                nodes_pointing_to_node
                    .get(&node_id)
                    .unwrap_or(&Vec::new())
                    .clone();
            neighbors.extend(incoming.iter());

            if neighbors.len() == 2 && incoming.len() == outgoing.len() {
                let two_way = incoming.len() == 2;
                if !two_way {
                    let pred = nodes_pointing_to_node.get(&node_id).unwrap()[0];
                    let succ = edges[0].node;
                    let cost = edges[0].cost + minimized_graph.get(&pred).unwrap().iter().find(|x| x.node == node_id).unwrap().cost;
                    let new_edge = Edge::new(succ, cost); 
                    let mut pred_edges = minimized_graph.get_mut(&pred).unwrap().clone();
                    pred_edges.retain(|x| x.node != node_id);
                    pred_edges.push(new_edge);
                    minimized_graph.remove(&node_id);
                    minimized_graph.insert(pred, pred_edges.to_vec());
                    nodes_pointing_to_node.get_mut(&succ).unwrap().retain(|x| *x != node_id);
                    nodes_pointing_to_node.get_mut(&succ).unwrap().push(pred);
                }   
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

#[test]
fn one_way_roads_minimization() {
    let mut graph: HashMap<NodeId, Vec<Edge>> = HashMap::new();
    graph.insert(NodeId(1), vec![Edge::new(NodeId(2), 1)]);
    graph.insert(NodeId(2), vec![Edge::new(NodeId(3), 1)]);
    let minimized_graph = FullGraph::minimize_graph(graph);
    assert_eq!(minimized_graph.len(), 1);
    assert_eq!(minimized_graph.get(&NodeId(1)).unwrap()[0].node, NodeId(3));
    assert_eq!(minimized_graph.get(&NodeId(1)).unwrap()[0].cost, 2);

}

#[test]
fn one_way_roads_minimization_long() {
    let mut graph: HashMap<NodeId, Vec<Edge>> = HashMap::new();
    graph.insert(NodeId(1), vec![Edge::new(NodeId(2), 1)]);
    graph.insert(NodeId(2), vec![Edge::new(NodeId(3), 1)]);
    graph.insert(NodeId(3), vec![Edge::new(NodeId(4), 1)]);
    graph.insert(NodeId(4), vec![Edge::new(NodeId(5), 1)]);
    let minimized_graph = FullGraph::minimize_graph(graph);
    assert_eq!(minimized_graph.len(), 1);
    assert_eq!(minimized_graph.get(&NodeId(1)).unwrap()[0].node, NodeId(5));
    assert_eq!(minimized_graph.get(&NodeId(1)).unwrap()[0].cost, 4);
}

#[test]
fn one_way_roads_with_cross(){
    let mut graph: HashMap<NodeId, Vec<Edge>> = HashMap::new();
    graph.insert(NodeId(1), vec![Edge::new(NodeId(2), 1)]);
    graph.insert(NodeId(2), vec![Edge::new(NodeId(3), 1)]);
    graph.insert(NodeId(3), vec![Edge::new(NodeId(4), 1)]);
    graph.insert(NodeId(4), vec![Edge::new(NodeId(5), 1)]);
    graph.insert(NodeId(5), Vec::new());
    graph.insert(NodeId(6), vec![Edge::new(NodeId(4), 1)]);
    graph.insert(NodeId(7), vec![Edge::new(NodeId(6), 1)]);


    let minimized_graph = FullGraph::minimize_graph(graph);
    assert_eq!(minimized_graph.len(), 4);

}