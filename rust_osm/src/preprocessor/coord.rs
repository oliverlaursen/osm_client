use serde::Serialize;

#[derive(Debug, Clone, Copy, Serialize)]
pub struct Coord {
    pub lat: f64,
    pub lon: f64,
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