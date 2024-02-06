// Copyright (c) 2014-2015 Guillaume Pinot <texitoi(a)texitoi.eu>
//
// This work is free. You can redistribute it and/or modify it under
// the terms of the Do What The Fuck You Want To Public License,
// Version 2, as published by Sam Hocevar. See the COPYING file for
// more details.

use osmpbfreader::{NodeId, WayId};
use rayon::prelude::*;
use serde::Serialize;
use serde_json;
use std::{
    cmp::Ordering,
    collections::{BinaryHeap, HashMap, HashSet},
    f64::consts::PI,
    io::Write,
};

#[derive(Serialize)]
pub struct FullGraph {
    graph: HashMap<NodeId, Vec<Edge>>,
    nodes: HashMap<NodeId, (f64, f64)>,
    ways: HashMap<WayId, Vec<NodeId>>,
}

#[derive(Debug, Clone, Serialize)]
pub struct Node {
    id: NodeId,
    coord: Coord,
}

#[derive(Debug, Clone, Copy, Serialize)]
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

#[derive(Clone, Debug, Eq, PartialEq, Serialize)]
pub struct Edge {
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
    let mut roads_associated_with_node: HashMap<NodeId, Vec<WayId>> = HashMap::new();
    for road in roads {
        for node in &road.node_refs {
            roads_associated_with_node
                .entry(*node)
                .or_insert(Vec::new())
                .push(road.id);
        }
    }
    // For each road, add the next node to the graph
    for node in roads_associated_with_node {
        let mut edges: Vec<Edge> = Vec::new();
        for road_id in node.1 {
            let road = roads.iter().find(|r| r.id == road_id).unwrap();
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

fn dijkstra(
    graph: &HashMap<NodeId, Vec<Edge>>,
    start: NodeId,
    goal: NodeId,
) -> (Option<u32>, Vec<NodeId>) {
    let mut dist: HashMap<NodeId, u32> = HashMap::new();
    let mut heap = BinaryHeap::new();

    dist.insert(start, 0);
    heap.push(Edge {
        node: start,
        cost: 0,
    });

    while let Some(Edge { node, cost }) = heap.pop() {
        if node == goal {
            return (Some(cost), dist.keys().cloned().collect());
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

    (None, vec![])
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
        let nodes_to_keep_hashset = HashSet::from_par_iter(nodes_to_keep);
        let nodes_hashmap = nodes
            .par_iter()
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

impl Preprocessor {
    pub fn new() -> Self {
        Preprocessor {
            nodes_to_keep: HashSet::new(),
            nodes: HashMap::new(),
            roads: Vec::new(),
        }
    }

    pub fn write_full_graph(graph: FullGraph, filename: &str) {
        let mut file = std::fs::File::create(filename).unwrap();
        let serialized = serde_json::to_string(&graph).unwrap();
        file.write_all(serialized.as_bytes()).unwrap();
    }

    pub fn project_nodes_to_2d(&self) -> HashMap<NodeId, (f64, f64)> {
        let center_point = self.nodes.iter().fold((0.0, 0.0), |acc, (_, node)| {
            (acc.0 + node.coord.lat, acc.1 + node.coord.lon)
        });
        let center_point = (
            center_point.0 / self.nodes.len() as f64,
            center_point.1 / self.nodes.len() as f64,
        );
        let projected_points = self
            .nodes
            .par_iter()
            .map(|(nodeid, node)| {
                let projected = azimuthal_equidistant_projection(node.coord, center_point);
                (*nodeid, projected)
            })
            .collect();
        projected_points
    }
}

fn azimuthal_equidistant_projection(coord: Coord, center: (f64, f64)) -> (f64, f64) {
    let lat_rad = coord.lat * (PI / 180.0);
    let lon_rad = coord.lon * (PI / 180.0);
    let center_lat_rad = center.0 * (PI / 180.0);
    let center_lon_rad = center.1 * (PI / 180.0);

    let r = 6371000.0;

    let delta_lon = lon_rad - center_lon_rad;
    let central_angle = (center_lat_rad.sin() * lat_rad.sin()
        + center_lat_rad.cos() * lat_rad.cos() * delta_lon.cos())
    .acos();

    let distance = r * central_angle;

    let azimuth = delta_lon
        .sin()
        .atan2(center_lat_rad.cos() * lat_rad.tan() - center_lat_rad.sin() * delta_lon.cos());

    let x = distance * azimuth.sin();
    let y = distance * azimuth.cos();

    (x, y)
}

fn main() {
    let time = std::time::Instant::now();

    let mut preprocessor =
        Preprocessor::get_roads_and_nodes(is_valid_highway, "src/test_data/denmark.osm.pbf");
    preprocessor.filter_nodes();
    let graph = build_graph(&preprocessor.nodes, &preprocessor.roads);

    let projected_points: HashMap<NodeId, (f64, f64)> = preprocessor.project_nodes_to_2d();
    let roads: HashMap<WayId, Vec<NodeId>> = preprocessor
        .roads
        .par_iter()
        .map(|road| (road.id, road.node_refs.clone()))
        .collect();
    let full_graph = FullGraph {
        graph,
        nodes: projected_points,
        ways: roads,
    };
    Preprocessor::write_full_graph(full_graph, "../OSM_UNITY_CLIENT/Assets/Maps/denmark.json");
    println!("Time: {:?}", time.elapsed());
}

//TESTS
fn initialize(filename: &str) -> Preprocessor {
    let mut preprocessor = Preprocessor::get_roads_and_nodes(is_valid_highway, filename);
    preprocessor.filter_nodes();
    preprocessor
}

#[test]
fn test_real_all() {
    //checks if file has been parsed correctly, with 2 nodes and 1 road
    let preprocessor = initialize("src/test_data/minimal.osm.testpbf");
    assert_eq!(1, preprocessor.roads.len());
    assert_eq!(2, preprocessor.nodes.len());
}

#[test]
fn road_is_oneway() {
    //checks if road is a oneway road
    let preprocessor = initialize("src/test_data/minimal.osm.testpbf");
    assert_eq!(CarDirection::FORWARD, preprocessor.roads[0].direction);
}

#[test]
fn does_not_include_blacklisted_roads() {
    //length of the road list should be 1, since one of the roads is pedestrian, which is blacklisted
    let preprocessor = initialize("src/test_data/minimal_ignored_road.osm.testpbf");
    assert_eq!(1, preprocessor.roads.len());
}

#[test]
fn one_node_is_dropped() {
    //amount of nodes kept are 2, because one node is not referenced by a road
    let preprocessor = initialize("src/test_data/one_node_is_dropped.osm.testpbf");
    assert_eq!(2, preprocessor.nodes.len());
}

#[test]
fn all_nodes_to_keep_are_kept() {
    //checks that all nodes in nodes kept is also in nodes to keep.
    let preprocessor = initialize("src/test_data/minimal.osm.testpbf");
    let nodes_to_keep = &preprocessor.nodes_to_keep;
    let nodes_kept = &preprocessor.nodes;
    for node in nodes_kept {
        assert_eq!(true, nodes_to_keep.contains(node.0));
    }
}

#[test]
fn dijkstra_test() {
    //distance is noted down as 206m from open street map. 2% error margin allowed
    let preprocessor = initialize("src/test_data/ribe_slice.osm.testpbf");
    let graph = build_graph(&preprocessor.nodes, &preprocessor.roads);
    let start = NodeId(603896384); //seminarievej
    let goal = NodeId(603896385); //Drost peders vej
    let (cost, _) = dijkstra(&graph, start, goal);
    let cost = cost.unwrap() as f32;
    let measured_dist = 206.;
    let min_expect = measured_dist * 0.98;
    let max_expect = measured_dist * 1.02;
    assert_eq!(true, min_expect <= cost && cost <= max_expect);
}

//6600188499 la masana
//53275038 canillo

#[test]
fn bigger_dijkstra_test() {
    //distance is noted down as 13.7km from google maps. 2% error margin allowed
    let preprocessor = initialize("src/test_data/andorra.osm.testpbf");
    let graph = build_graph(&preprocessor.nodes, &preprocessor.roads);
    let start = NodeId(6600188499); //la masana
    let goal = NodeId(53275038); //canillo
    let (cost, _) = dijkstra(&graph, start, goal);
    let cost = cost.unwrap() as f32;
    let measured_dist = 13700.;
    let min_expect = measured_dist * 0.98;
    let max_expect = measured_dist * 1.02;

    println!("Shortest path from {} to {}: {} ", start.0, goal.0, cost);
    assert_eq!(true, min_expect <= cost && cost <= max_expect);
}
