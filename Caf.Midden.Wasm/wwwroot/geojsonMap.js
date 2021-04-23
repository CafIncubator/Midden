// Creates a leaflet map using USGS tile layers and adds the spatial extent as a geojson layer
export function create(mapElement, geometry) {

    // Creates a geojson feature from the geometry argument
    // TODO: Error handling
    var geojsonFeature = {
        "type": "Feature",
        "properties": {},
        "geometry": JSON.parse(geometry)
    };

    // Creates, and adds, tile layers with a tile control
    var usgsTopo = L.tileLayer('https://basemap.nationalmap.gov/arcgis/rest/services/USGSTopo/MapServer/tile/{z}/{y}/{x}', {
        maxZoom: 15,
        attribution: 'Tiles courtesy of the <a href="https://usgs.gov/">U.S. Geological Survey</a>'
    });

    var usgsSat = L.tileLayer('https://basemap.nationalmap.gov/arcgis/rest/services/USGSImageryTopo/MapServer/tile/{z}/{y}/{x}', {
        maxZoom: 15,
        attribution: 'Tiles courtesy of the <a href="https://usgs.gov/">U.S. Geological Survey</a>'
    });

    var leafletMap = L.map(mapElement.id, { layers: [usgsTopo, usgsSat ] });

    var baseMaps = {
        "Topo": usgsTopo,
        "Sat": usgsSat        
    };

    L.control.layers(baseMaps).addTo(leafletMap);

    // Adds geojson feature to the map and sets the map bounds to display the feature
    var spatialExtent = L.geoJSON().addTo(leafletMap);
    spatialExtent.addData(geojsonFeature);

    document.getElementById(mapElement.id).style.width = "100%";

    leafletMap.fitBounds(spatialExtent.getBounds());

    return leafletMap;
}