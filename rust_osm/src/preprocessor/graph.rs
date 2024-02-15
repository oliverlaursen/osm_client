use crate::preprocessor::edge::*;
use crate::preprocessor::preprocessor::*;

use osmpbfreader::{NodeId, WayId};
use rayon::iter::IntoParallelRefIterator;
use rayon::iter::ParallelIterator;
use std::collections::HashMap;
use std::collections::HashSet;

pub struct Graph;

impl Graph {
    pub fn minimize_graph(graph: &mut HashMap<NodeId, Vec<Edge>>, node_ids: Vec<NodeId>)  {
        let mut nodes_pointing_to_node: HashMap<NodeId, Vec<NodeId>> = HashMap::new();
        let mut intermediate_nodes: Vec<NodeId> = Vec::new();

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
        for node_id in node_ids {
            let edges = graph.get_mut(&node_id).unwrap();
            let mut neighbors: HashSet<NodeId> = edges.iter().map(|edge| edge.node).collect();
            let outgoing = neighbors.clone();
            let incoming = 
                nodes_pointing_to_node
                    .get(&node_id)
                    .unwrap_or(&Vec::new())
                    .clone();
            neighbors.extend(incoming.iter());

            if neighbors.len() == 2 && incoming.len() == outgoing.len() {
                intermediate_nodes.push(node_id);
            }
        }

        // Fix all intermediate nodes
        for node_id in &intermediate_nodes {
            let edges = graph.get_mut(&node_id).unwrap();
             {
                let node_id = *node_id;
                let outgoing: HashSet<NodeId> = edges.iter().map(|edge| edge.node).collect();
                let two_way = outgoing.len() == 2;
                if !two_way {
                    let pred = nodes_pointing_to_node.get(&node_id).unwrap()[0];
                    let succ = edges[0].node;
                    let cost = edges[0].cost + graph.get(&pred).unwrap().iter().find(|x| x.node == node_id).unwrap().cost;
                    let new_edge = Edge::new(succ, cost);
                    Graph::update_edges_and_remove_node(pred, node_id, graph, new_edge); 
                    Graph::update_nodes_pointing_to_node_edge(&succ, &mut nodes_pointing_to_node, &pred, node_id)
                }   else {
                    let succ = edges[0].node;
                    let pred = edges[1].node;
                    let cost = edges[0].cost + edges[1].cost;
                    let new_edge_from_pred = Edge::new(succ, cost);
                    let new_edge_from_succ = Edge::new(pred, cost);
                    Graph::update_edges_and_remove_node(pred, node_id, graph, new_edge_from_pred);
                    Graph::update_edges_and_remove_node(succ, node_id, graph, new_edge_from_succ);
                    Graph::update_nodes_pointing_to_node_edge(&pred, &mut nodes_pointing_to_node, &succ, node_id);
                    Graph::update_nodes_pointing_to_node_edge(&succ, &mut nodes_pointing_to_node, &pred, node_id);
                }
            }
        }
        // Remove loops
        
        for (node, edges) in graph.iter_mut() {
            edges.retain(|x| x.node != *node);
        }
    }

    fn update_edges_and_remove_node(pred: NodeId, node: NodeId, graph: &mut HashMap<NodeId, Vec<Edge>>, new_edge: Edge) {
        let mut pred_edges = graph.get_mut(&pred).expect(format!("Could not get edges from {:?}",&pred).as_str()).clone();
        pred_edges.retain(|x| x.node != node);
        pred_edges.push(new_edge);
        graph.remove(&node);
        graph.insert(pred, pred_edges.to_vec());
        
    }

    fn update_nodes_pointing_to_node_edge(from: &NodeId, nodes_pointing_to_node: &mut HashMap<NodeId, Vec<NodeId>>, to: &NodeId, intermediate:NodeId) -> (){
        let edges = nodes_pointing_to_node
                        .get_mut(&from)
                        .unwrap();
        edges.retain(|x| *x != intermediate);
        edges.push(*to);
        
    }

    pub fn build_graph(
        nodes: &HashMap<NodeId, Node>,
        roads: &Vec<Road>,
    ) -> HashMap<NodeId, Vec<Edge>> {
        let roads: HashMap<WayId, Road> = roads
            .par_iter()
            .map(|road| (road.id.clone(), road.clone()))
            .collect();
        let mut graph: HashMap<NodeId, Vec<Edge>> = HashMap::new();
        let mut roads_associated_with_node: HashMap<NodeId, Vec<WayId>> = HashMap::new();
        for road in &roads {
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
                if index != road.node_refs.len() - 1 {
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
    Graph::minimize_graph(&mut graph, node_ids);
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
    Graph::minimize_graph(&mut graph, node_ids);
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

    Graph::minimize_graph(&mut graph, node_ids);
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
    Graph::minimize_graph(&mut graph, node_ids);
    assert_eq!(graph.len(), 2);
    assert_eq!(graph.get(&NodeId(1)).unwrap()[0].node, NodeId(3));
    assert_eq!(graph.get(&NodeId(1)).unwrap()[0].cost, 2);
    assert_eq!(graph.get(&NodeId(3)).unwrap()[0].node, NodeId(1));
    assert_eq!(graph.get(&NodeId(3)).unwrap()[0].cost, 2);
}

#[test]
fn one_way_cycle(){
    let mut graph: HashMap<NodeId, Vec<Edge>> = HashMap::new();
    let node_ids = vec![NodeId(1), NodeId(2), NodeId(3), NodeId(4), NodeId(5)];
    graph.insert(NodeId(1), vec![Edge::new(NodeId(2), 1)]);
    graph.insert(NodeId(2), vec![Edge::new(NodeId(1), 1),Edge::new(NodeId(3), 1)]);
    graph.insert(NodeId(3), vec![Edge::new(NodeId(4), 1)]);
    graph.insert(NodeId(4), vec![Edge::new(NodeId(5), 1)]);
    graph.insert(NodeId(5), vec![Edge::new(NodeId(2), 1)]);
    Graph::minimize_graph(&mut graph,node_ids);
    println!("{:?}", graph);
}

#[test]
fn advanced_one_way_cycle(){
    let mut graph: HashMap<NodeId, Vec<Edge>> = HashMap::new();
    let node_ids = vec![NodeId(1), NodeId(2), NodeId(3), NodeId(4), NodeId(5), NodeId(6), NodeId(7), NodeId(8), NodeId(9), NodeId(10), NodeId(11)];
    graph.insert(NodeId(1), vec![Edge::new(NodeId(2), 1),Edge::new(NodeId(10), 1)]);
    graph.insert(NodeId(2), vec![Edge::new(NodeId(3), 1)]);
    graph.insert(NodeId(3), vec![Edge::new(NodeId(4), 1)]);
    graph.insert(NodeId(4), vec![Edge::new(NodeId(5), 1)]);
    graph.insert(NodeId(5), vec![Edge::new(NodeId(6), 1), Edge::new(NodeId(11), 1)]);
    graph.insert(NodeId(6), vec![Edge::new(NodeId(7), 1)]);
    graph.insert(NodeId(7), vec![Edge::new(NodeId(8), 1)]);
    graph.insert(NodeId(8), vec![Edge::new(NodeId(9), 1)]);
    graph.insert(NodeId(9), vec![Edge::new(NodeId(1), 1)]);
    graph.insert(NodeId(10), Vec::new());
    graph.insert(NodeId(11), Vec::new());
    Graph::minimize_graph(&mut graph,node_ids);
    println!("{:?}", graph);
}