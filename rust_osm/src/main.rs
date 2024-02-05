// Copyright (c) 2014-2015 Guillaume Pinot <texitoi(a)texitoi.eu>
//
// This work is free. You can redistribute it and/or modify it under
// the terms of the Do What The Fuck You Want To Public License,
// Version 2, as published by Sam Hocevar. See the COPYING file for
// more details.

use osmpbfreader::{blocks::nodes, NodeId, OsmId, WayId};
use rayon::prelude::*;
use std::{
    cmp::Ordering,
    collections::{BinaryHeap, HashMap, HashSet},
    hash, string,
};

#[macro_use]
extern crate osmpbfreader;
#[derive(Debug, Clone)]
pub struct Node {
    id: NodeId,
    coord: Coord,
}

#[derive(Debug, Clone, Copy)]
pub struct Coord {
    pub lat: f64,
    pub lon: f64,
}

#[derive(Debug)]
pub struct Road {
    id: WayId,
    node_refs: Vec<NodeId>,
    direction: CarDirection,
}

#[derive(Debug, PartialEq)]
pub enum CarDirection {
    FORWARD,
    TWOWAY,
}

pub struct Preprocessor {
    pub nodes_to_keep: HashSet<NodeId>,
    pub nodes: HashMap<NodeId, Node>,
    pub roads: Vec<Road>,
}

#[derive(Clone, Debug, Eq, PartialEq)]
struct Edge {
    node: NodeId,
    cost: u32, // This could be distance, time, etc.
}

impl Ord for Edge {
    fn cmp(&self, other: &Self) -> Ordering {
        // Notice we flip the ordering here because BinaryHeap is a max heap by default
        other.cost.cmp(&self.cost)
    }
}

impl PartialOrd for Edge {
    fn partial_cmp(&self, other: &Self) -> Option<Ordering> {
        Some(self.cmp(other))
    }
}

impl Coord {
    pub fn distance_to(&self, end: Coord) -> f64 {
        let r: f64 = 6_378_100.0;

        let dlon: f64 = (end.lon - self.lon).to_radians();
        let dlat: f64 = (end.lat - self.lat).to_radians();
        let lat1: f64 = (self.lat).to_radians();
        let lat2: f64 = (end.lat).to_radians();

        let a: f64 = ((dlat / 2.0).sin()) * ((dlat / 2.0).sin())
            + ((dlon / 2.0).sin()) * ((dlon / 2.0).sin()) * (lat1.cos()) * (lat2.cos());
        let c: f64 = 2.0 * ((a.sqrt()).atan2((1.0 - a).sqrt()));

        r * c
    }
}

fn build_graph(nodes: &HashMap<NodeId, Node>, roads: &[Road]) -> HashMap<NodeId, Vec<Edge>> {
    let mut graph: HashMap<NodeId, Vec<Edge>> = HashMap::new();
    for road in roads {
        for window in road.node_refs.windows(2) {
            let from = window[0];
            let to = window[1];
            let from_coord = nodes.get(&from).unwrap().coord;
            let to_coord = nodes.get(&to).unwrap().coord;
            let cost = from_coord.distance_to(to_coord) as u32;
            graph
                .entry(from)
                .or_insert_with(Vec::new)
                .push(Edge { node: to, cost });

            if matches!(road.direction, CarDirection::TWOWAY) {
                graph
                    .entry(to)
                    .or_insert_with(Vec::new)
                    .push(Edge { node: from, cost });
            }
        }
    }
    graph
}

fn dijkstra(graph: &HashMap<NodeId, Vec<Edge>>, start: NodeId, goal: NodeId) -> Option<u32> {
    let mut dist: HashMap<NodeId, u32> = HashMap::new();
    let mut heap = BinaryHeap::new();

    dist.insert(start, 0);
    heap.push(Edge {
        node: start,
        cost: 0,
    });

    while let Some(Edge { node, cost }) = heap.pop() {
        if node == goal {
            return Some(cost);
        }

        if cost > *dist.get(&node).unwrap_or(&u32::MAX) {
            continue;
        }

        for edge in &graph[&node] {
            let next = Edge {
                node: edge.node,
                cost: cost + edge.cost,
            };
            if next.cost < *dist.get(&next.node).unwrap_or(&u32::MAX) {
                heap.push(next.clone());
                dist.insert(next.node, next.cost);
            }
        }
    }

    None
}

// Add your nodes and roads initialization here, and then call build_graph and dijkstra accordingly.

impl Preprocessor {
    pub fn get_roads_and_nodes(
        is_valid_highway: fn(&osmpbfreader::Tags, &HashSet<&str>) -> bool,
        filename: &str,
    ) -> Self {
        let blacklist: HashSet<&str> = HashSet::from_iter([
            "pedestrian",
            "footway",
            "steps",
            "path",
            "cycleway",
            "proposed",
            "construction",
            "bridleway",
            "abandoned",
            "platform",
            "raceway",
            "service",
            "services",
            "rest_area",
            "escape",
            "raceway",
            "busway",
            "footway",
            "bridlway",
            "steps",
            "corridor",
            "via_ferreta",
            "sidewalk",
            "crossing",
            "proposed",
            "track",
        ]);
        let r = std::fs::File::open(&std::path::Path::new(filename)).unwrap();
        let mut pbf = osmpbfreader::OsmPbfReader::new(r);
        let mut nodes: Vec<Node> = Vec::new();
        let mut roads: Vec<Road> = Vec::new();
        let mut nodes_to_keep: Vec<NodeId> = Vec::new();
        for obj in pbf.par_iter().map(Result::unwrap) {
            match obj {
                osmpbfreader::OsmObj::Node(node) => nodes.push(Node {
                    id: node.id,
                    coord: Coord {
                        lat: node.lat(),
                        lon: node.lon(),
                    },
                }),
                osmpbfreader::OsmObj::Way(way) => {
                    if !is_valid_highway(&way.tags, &blacklist) {
                        continue;
                    }
                    nodes_to_keep.extend(&way.nodes);
                    roads.push(Road {
                        id: way.id,
                        node_refs: way.nodes,
                        direction: way.tags.get("oneway").map_or(CarDirection::TWOWAY, |v| {
                            if v == "yes" {
                                CarDirection::FORWARD
                            } else {
                                CarDirection::TWOWAY
                            }
                        }),
                    })
                }
                osmpbfreader::OsmObj::Relation(_) => (),
            }
        }
        let nodes_to_keep_hashset = HashSet::from_iter(nodes_to_keep);
        let nodes_hashmap = nodes
            .iter()
            .map(|node| (node.id, node.clone()))
            .collect::<HashMap<NodeId, Node>>();

        Preprocessor {
            nodes_to_keep: nodes_to_keep_hashset,
            nodes: nodes_hashmap,
            roads,
        }
    }

    pub fn filter_nodes(&mut self) {
        // Filter out nodes that are not in nodes_to_keep
        let nodes = self
            .nodes
            .par_iter() // Use a parallel iterator
            .map(|(nodeid, node)| (*nodeid, node.clone())) // Dereference the tuple elements before cloning
            .filter(|(nodeid, _)| self.nodes_to_keep.contains(nodeid))
            .collect();
        self.nodes = nodes;
    }
}
fn is_valid_highway(tags: &osmpbfreader::Tags, blacklist: &HashSet<&str>) -> bool {
    tags.iter()
        .any(|(k, v)| (k == "highway" && !blacklist.contains(v.as_str())))
}

fn main() {
    let time = std::time::Instant::now();

    let mut preprocessor = Preprocessor::get_roads_and_nodes(is_valid_highway, "andorra.osm.pbf");
    preprocessor.filter_nodes();
    println!("Nodes: {:?}", preprocessor.nodes.len());
    println!("Roads: {:?}", preprocessor.roads.len());
    println!("Time: {:?}", time.elapsed());

    let graph = build_graph(&preprocessor.nodes, &preprocessor.roads);
    let start = NodeId(51446486);
    let goal = NodeId(2021666213);
    let cost = dijkstra(&graph, start, goal);
    println!(
        "Shortest path from {} to {}: {} ",
        start.0,
        goal.0,
        cost.unwrap_or(0)
    );
}


//TESTS
fn initialize(filename: &str) -> Preprocessor {
    let mut preprocessor = Preprocessor::get_roads_and_nodes(is_valid_highway, filename);
    preprocessor.filter_nodes();
    preprocessor
}

#[test]
fn test_real_all() { //checks if file has been parsed correctly, with 2 nodes and 1 road
    let preprocessor = initialize("src/test_data/minimal.osm.testpbf");
    assert_eq!(1, preprocessor.roads.len());
    assert_eq!(2, preprocessor.nodes.len());
}

#[test]
fn road_is_oneway() { //checks if road is a oneway road
    let preprocessor = initialize("src/test_data/minimal.osm.testpbf");
    assert_eq!(CarDirection::FORWARD, preprocessor.roads[0].direction);
}

#[test]
fn does_not_include_blacklisted_roads() { //length of the road list should be 1, since one of the roads is pedestrian, which is blacklisted
    let preprocessor = initialize("src/test_data/minimal_ignored_road.osm.testpbf");    
    assert_eq!(1, preprocessor.roads.len());
}

#[test]
fn one_node_is_dropped() { //amount of nodes kept are 2, because one node is not referenced by a road
    let preprocessor = initialize("src/test_data/one_node_is_dropped.osm.testpbf");
    assert_eq!(2, preprocessor.nodes.len());
}

#[test]
fn all_nodes_to_keep_are_kept() { //checks that all nodes in nodes kept is also in nodes to keep.
    let preprocessor = initialize("src/test_data/minimal.osm.testpbf");
    let nodes_to_keep = &preprocessor.nodes_to_keep;
    let nodes_kept = &preprocessor.nodes;
    for node in nodes_kept {
        assert_eq!(true, nodes_to_keep.contains(node.0));
    }
}

#[test]
fn dijkstra_test() { //distance is noted down as 206m from open street map. 2% error margin allowed
    let preprocessor = initialize("src/test_data/ribe_slice.osm.testpbf");
    let graph = build_graph(&preprocessor.nodes, &preprocessor.roads);
    let start = NodeId(603896384); //seminarievej
    let goal = NodeId(603896385 ); //Drost peders vej
    let cost = dijkstra(&graph, start, goal).unwrap() as f32;
    let measured_dist = 206.;
    let min_expect = measured_dist * 0.98;
    let max_expect = measured_dist * 1.02;
    assert_eq!(true, min_expect <= cost && cost <= max_expect);

}