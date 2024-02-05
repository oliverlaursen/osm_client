// Copyright (c) 2014-2015 Guillaume Pinot <texitoi(a)texitoi.eu>
//
// This work is free. You can redistribute it and/or modify it under
// the terms of the Do What The Fuck You Want To Public License,
// Version 2, as published by Sam Hocevar. See the COPYING file for
// more details.

use std::{collections::HashSet, hash, string};

use osmpbfreader::{NodeId, OsmId, WayId};

#[macro_use]
extern crate osmpbfreader;
#[derive(Debug)]
pub struct Node {
    id: NodeId,
    lat: f64,
    lon: f64,
}
#[derive(Debug)]
pub struct Road {
    id: WayId,
    node_refs: Vec<NodeId>
}

pub fn get_roads_and_nodes(filter: fn(&osmpbfreader::Tags, &HashSet<&str>) -> bool, filename: &str) -> (Vec<Node>, Vec<Road>) {
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
    for obj in pbf.par_iter().map(Result::unwrap) {
        if !filter(obj.tags(), &blacklist) {
            continue;
        }
        match obj {
            osmpbfreader::OsmObj::Node(node) => {
                nodes.push(Node { id: node.id, lat: node.lat(), lon: node.lon() })
            }
            osmpbfreader::OsmObj::Way(way) => {
                roads.push(Road { id: way.id, node_refs: way.nodes })
            }
            osmpbfreader::OsmObj::Relation(_) => {
                ()
            }
        }
    }
    return (nodes, roads);
}

fn filter(tags: &osmpbfreader::Tags, blacklist: &HashSet<&str>) -> bool {
    
    for (k, v) in tags.iter() {
        if k == "highway" && blacklist.contains(v.as_str()) {
            return false;
        } else {
            return true;}
    }
    return true;
}

fn main() {
    let time = std::time::Instant::now();
    let (nodes, roads) = get_roads_and_nodes(filter, "denmark.osm.pbf");
    println!("Nodes: {:?}", nodes.len());  
    println!("Roads: {:?}", roads.len());
    println!("Time: {:?}", time.elapsed());
}
