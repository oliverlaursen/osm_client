// Copyright (c) 2014-2015 Guillaume Pinot <texitoi(a)texitoi.eu>
//
// This work is free. You can redistribute it and/or modify it under
// the terms of the Do What The Fuck You Want To Public License,
// Version 2, as published by Sam Hocevar. See the COPYING file for
// more details.

use std::{collections::HashSet, hash, string};
use rayon::prelude::*;
use osmpbfreader::{NodeId, OsmId, WayId};

#[macro_use]
extern crate osmpbfreader;
#[derive(Debug,Clone)]
pub struct Node {
    id: NodeId,
    lat: f64,
    lon: f64,
}
#[derive(Debug)]
pub struct Road {
    id: WayId,
    node_refs: Vec<NodeId>,
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
            if !is_valid_highway(obj.tags(), &blacklist) {
                continue;
            }
            match obj {
                osmpbfreader::OsmObj::Node(node) => nodes.push(Node {
                    id: node.id,
                    lat: node.lat(),
                    lon: node.lon(),
                }),
                osmpbfreader::OsmObj::Way(way) => {
                    nodes_to_keep.extend(&way.nodes);
                    roads.push(Road {
                        id: way.id,
                        node_refs: way.nodes,
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

    pub fn get_nodes(&mut self) {
        println!("NODES {:?}", self.nodes.len());
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
    tags.iter().any(|(k,v)| (k == "highway" && !blacklist.contains(v.as_str())))
}

fn main() {
    let time = std::time::Instant::now();

    let mut preprocessor = Preprocessor::get_roads_and_nodes(is_valid_highway, "andorra.osm.pbf");
    preprocessor.get_nodes();
    println!("Nodes: {:?}", preprocessor.nodes.len());
    println!("Roads: {:?}", preprocessor.roads.len());
    println!("Time: {:?}", time.elapsed());
}

#[test]
fn test_real_all() {
    let mut preprocessor = Preprocessor::get_roads_and_nodes(is_valid_highway, "minimal.osm.pbf");
    preprocessor.get_nodes();
    println!("nodes to keep {:?}", preprocessor.nodes_to_keep.len());
    println!("nodes: {}, roads: {}", preprocessor.nodes.len(), preprocessor.roads.len());
    assert_eq!(1, preprocessor.roads.len());
    assert_eq!(2, preprocessor.nodes.len());
}
