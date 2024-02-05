// Copyright (c) 2014-2015 Guillaume Pinot <texitoi(a)texitoi.eu>
//
// This work is free. You can redistribute it and/or modify it under
// the terms of the Do What The Fuck You Want To Public License,
// Version 2, as published by Sam Hocevar. See the COPYING file for
// more details.

use osmpbfreader::{NodeId, OsmId, WayId};
use rayon::prelude::*;
use std::{collections::HashSet, hash, string};

#[macro_use]
extern crate osmpbfreader;
#[derive(Debug, Clone)]
pub struct Node {
    id: NodeId,
    lat: f64,
    lon: f64,
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
    pub nodes: Vec<Node>,
    pub roads: Vec<Road>,
}

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
                    lat: node.lat(),
                    lon: node.lon(),
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

        Preprocessor {
            nodes_to_keep: nodes_to_keep_hashset,
            nodes,
            roads,
        }
    }

    pub fn filter_nodes(&mut self) {
        // Filter out nodes that are not in nodes_to_keep
        let nodes = self
            .nodes
            .par_iter() // Use a parallel iterator
            .filter(|node| self.nodes_to_keep.contains(&node.id))
            .cloned()
            .collect::<Vec<Node>>();
        self.nodes = nodes;
    }
}
fn is_valid_highway(tags: &osmpbfreader::Tags, blacklist: &HashSet<&str>) -> bool {
    tags.iter()
        .any(|(k, v)| (k == "highway" && !blacklist.contains(v.as_str())))
}

fn main() {
    let time = std::time::Instant::now();

    let mut preprocessor = Preprocessor::get_roads_and_nodes(is_valid_highway, "denmark.osm.pbf");
    preprocessor.filter_nodes();
    println!("Nodes: {:?}", preprocessor.nodes.len());
    println!("Roads: {:?}", preprocessor.roads.len());
    println!("Time: {:?}", time.elapsed());
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
/* 
#[test]
fn all_nodes_to_keep_are_kept() {
    let preprocessor = initialize("src/test_data/minimal.osm.testpbf");
    let nodes_to_keep = &preprocessor.nodes_to_keep;
    let nodes_kept = &preprocessor.nodes;
} */