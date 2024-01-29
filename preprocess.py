import xml.etree.ElementTree as ET
import json
import math

def haversine(lat1, lon1, lat2, lon2):
    # Earth radius in meters
    R = 6371000  

    # Convert latitude and longitude from degrees to radians
    lat1, lon1, lat2, lon2 = map(math.radians, [lat1, lon1, lat2, lon2])

    # Calculate differences
    dlat = lat2 - lat1
    dlon = lon2 - lon1

    # Haversine formula
    a = math.sin(dlat / 2) ** 2 + math.cos(lat1) * math.cos(lat2) * math.sin(dlon / 2) ** 2
    c = 2 * math.atan2(math.sqrt(a), math.sqrt(1 - a))

    # Distance in meters
    distance = R * c
    return distance

def lat_lon_to_cartesian(zero_lat, zero_lon, lat, lon):
    y = haversine(zero_lat, zero_lon, lat, zero_lon)
    x = haversine(zero_lat, zero_lon, zero_lat, lon)
    
    # Flip the sign of y to satisfy Unity
    x = -x
    return x, y

def preprocess_osm_data(file_path, output_file):
    # Parse the XML file
    tree = ET.parse(file_path)
    root = tree.getroot()
    first_node = root.find('node')
    zero_lat = float(first_node.get('lat'))
    zero_lon = float(first_node.get('lon'))

    nodes = []
    ways = []

    # Process nodes
    for node in root.findall('node'):
        node_id = node.get('id')
        lat = float(node.get('lat'))
        lon = float(node.get('lon'))
        x, y = lat_lon_to_cartesian(zero_lat, zero_lon, lat, lon)
        nodes.append({'id': node_id, 'x': x, 'y': y})

    # Process ways
    for way in root.findall('way'):
        # Ensure way is highway
        if way.find('tag[@k="highway"]') is None:
            continue
        way_id = way.get('id')
        nd_refs = [nd.get('ref') for nd in way.findall('nd')]
        ways.append({'id': way_id, 'node_refs': nd_refs})

    # Combined data
    simplified_data = {'nodes': nodes, 'ways': ways}

    # Write the simplified data to a JSON file
    with open(output_file, 'w') as outfile:
        json.dump(simplified_data, outfile, indent=4)

    print(f"Data preprocessed and saved to {output_file}")

if __name__ == '__main__':
    file_path = 'maps/samsø2.osm'  # Replace with your OSM data file path
    output_path = 'maps/samsø2.json'  # Replace with your output file path
    preprocessed_data = preprocess_osm_data(file_path, output_path)
