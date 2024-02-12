use crate::preprocessor::preprocessor::*;
use crate::preprocessor::edge::*;

use std::collections::HashMap;
use osmpbfreader::{NodeId, WayId};
use rayon::iter::IntoParallelRefIterator;
use rayon::iter::ParallelIterator;
use serde::Serialize;

#[derive(Serialize)]
pub struct FullGraph {
    graph: HashMap<NodeId, Vec<Edge>>,
    nodes: HashMap<NodeId, (f64, f64)>,
}

impl FullGraph {
    pub fn new(graph: HashMap<NodeId, Vec<Edge>>, nodes: HashMap<NodeId, (f64, f64)>) -> Self {
        FullGraph {
            graph,
            nodes,
        }
    }

    pub fn build_full_graph(preprocessor: &Preprocessor) -> FullGraph {
        let time = std::time::Instant::now();
        let projected_points: HashMap<NodeId, (f64, f64)> = preprocessor.project_nodes_to_2d();
        println!("Time to project nodes: {:?}", time.elapsed());
        let time = std::time::Instant::now();
        let roads: HashMap<WayId, Road> = preprocessor
            .roads
            .par_iter()
            .map(|road| (road.id.clone(), road.clone()))
            .collect();

        let graph = FullGraph::build_graph(&preprocessor.nodes, &roads);
        println!("Time to build graph: {:?}", time.elapsed());

        FullGraph::new(graph, projected_points)
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

