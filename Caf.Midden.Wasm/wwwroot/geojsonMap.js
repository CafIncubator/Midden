// Creates the USGS tile layers shared by the read-only and editable maps
export function createBaseLayers() {
    var usgsSat = L.tileLayer('https://basemap.nationalmap.gov/arcgis/rest/services/USGSImageryTopo/MapServer/tile/{z}/{y}/{x}', {
        maxNativeZoom: 15,
        maxZoom: 19,
        attribution: 'Tiles courtesy of the <a href="https://usgs.gov/">U.S. Geological Survey</a>'
    });

    var usgsTopo = L.tileLayer('https://basemap.nationalmap.gov/arcgis/rest/services/USGSTopo/MapServer/tile/{z}/{y}/{x}', {
        maxNativeZoom: 15,
        maxZoom: 19,
        attribution: 'Tiles courtesy of the <a href="https://usgs.gov/">U.S. Geological Survey</a>'
    });

    var esriImagery = L.tileLayer('https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}', {
        maxZoom: 19,
        attribution: 'Tiles &copy; <a href="https://www.esri.com/">Esri</a> &mdash; Source: Esri, Maxar, Earthstar Geographics'
    });

    var osm = L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        maxZoom: 19,
        attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
    });

    return {
        layers: [usgsTopo],
        baseMaps: {
            'USGS Topo': usgsTopo,
            'USGS Imagery Topo': usgsSat,
            'ESRI Imagery': esriImagery,
            'OpenStreetMap': osm
        }
    };
}

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
    var baseLayers = createBaseLayers();

    // GeoJsonMap.razor uses a fixed id="map"; use the id string so Leaflet
    // initialises exactly as it did before the editable-map refactoring
    var leafletMap = L.map(mapElement.id, { layers: baseLayers.layers });

    L.control.layers(baseLayers.baseMaps).addTo(leafletMap);

    // Adds geojson feature to the map and sets the map bounds to display the feature
    var spatialExtent = L.geoJSON().addTo(leafletMap);
    spatialExtent.addData(geojsonFeature);

    document.getElementById(mapElement.id).style.width = "100%";

    leafletMap.fitBounds(spatialExtent.getBounds());

    return leafletMap;
}
