// Copyright (c) 2014-2015 Guillaume Pinot <texitoi(a)texitoi.eu>
//
// This work is free. You can redistribute it and/or modify it under
// the terms of the Do What The Fuck You Want To Public License,
// Version 2, as published by Sam Hocevar. See the COPYING file for
// more details.
mod preprocessor;

use crate::preprocessor::coord::*;
use crate::preprocessor::graph::*;
use crate::preprocessor::preprocessor::*;

use std::f64::consts::PI;

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
    // TODO: Remove state (nodes and roads) to be able to drop()
    let (nodes_to_keep, mut nodes, mut roads) = Preprocessor::get_roads_and_nodes("src/test_data/andorra.osm.testpbf");
    Preprocessor::filter_nodes(&mut nodes, &nodes_to_keep);
    Preprocessor::rewrite_ids(&mut nodes, &mut roads);
    println!("Time to get roads and nodes: {:?}", time.elapsed());
    let graph = Preprocessor::build_graph_interwrites(nodes, roads);
    let projected_points = Preprocessor::project_nodes_to_2d_interwrites("node_coordinates_temp.txt");
    let time2 = std::time::Instant::now();
    println!("Length of projected points: {}", projected_points.len());
    Preprocessor::write_graph(projected_points, graph, "denmark.graph");
    println!("Time to write graph: {:?}", time2.elapsed());
    println!("Total time: {:?}", time.elapsed());
}
