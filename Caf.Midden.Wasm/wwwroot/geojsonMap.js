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
        layers: [usgsSat],
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

    // Creates a geojson feature from the geometry argument. The Blazor component reports parse
    // or rendering failures without preventing the rest of the metadata view from loading.
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

    var geometryType = geojsonFeature.geometry && geojsonFeature.geometry.type;
    var isPointGeometry = geometryType === "Point" || geometryType === "MultiPoint";

    leafletMap.fitBounds(
        spatialExtent.getBounds(),
        isPointGeometry ? { maxZoom: 15 } : undefined);

    return leafletMap;
}

// Tears a map down so Leaflet clears the "already initialized" marker it puts on the
// container element. Without this, navigating back to a page that reuses the same
// container throws "Map container is already initialized".
export function destroy(leafletMap) {
    if (leafletMap) {
        leafletMap.remove();
    }
}

// Creates a leaflet map showing the extents
// spatial coverage view. Boxes are pre-computed server-side as west/south/east/north, so there
// is no geometry to parse here and each dataset costs exactly one shape.
//
// Two renderings of the same boxes are added as toggleable overlays via the layer control, so
// the "outlined extents" and "density heatmap" treatments can be compared side by side:
//   - Extents: each box drawn as an outlined, semi-transparent rectangle. Precise per-dataset
//     boundaries, but overlap reads as a tangle of borders once dataset count grows.
//   - Coverage heatmap: box centers accumulated through Leaflet.heat, weighted by box count so
//     areas with more datasets glow brighter. No per-shape borders, so overlap reads cleanly as
//     density instead of clutter, at the cost of exact boundaries. minOpacity keeps even a single
//     lightly-weighted point clearly visible against busy basemap tiles when zoomed out, instead
//     of fading into near-invisibility the way Leaflet.heat's defaults do for sparse points.
//
// This widget defaults to the heatmap overlay (extents available but off) on the OpenStreetMap
// base layer: the heatmap is the primary "where is our data" signal for this card, and the plain
// OSM tiles keep the map legible without competing with satellite/topo imagery detail.
//
// Boxes flagged isPoint have no area (point geometries, axis-aligned lines) and would render as
// an invisible zero-size rectangle, so they are drawn as circle markers instead. Markers keep a
// constant screen size at any zoom, which is the honest representation: the dataset claims a
// location, not a region. Point boxes are also fed into the heatmap using their exact location.
export function createBoxes(mapElement, boxes) {
    var baseLayers = createBaseLayers();

    var leafletMap = L.map(mapElement.id, { layers: [baseLayers.baseMaps['OpenStreetMap']] });

    var extentsGroup = L.featureGroup();
    var heatPoints = [];

    (boxes || []).forEach(function (box) {
        if (box.isPoint) {
            L.circleMarker(
                [box.centerLatitude, box.centerLongitude],
                { radius: 4, color: '#1890ff', weight: 1, fillOpacity: 0.6 })
                .addTo(extentsGroup);
            heatPoints.push([box.centerLatitude, box.centerLongitude, 1]);
        } else {
            L.rectangle(
                [[box.south, box.west], [box.north, box.east]],
                { color: '#1890ff', weight: 1, fillOpacity: 0.15 })
                .addTo(extentsGroup);

            var centerLat = (box.south + box.north) / 2;
            var centerLng = (box.west + box.east) / 2;
            heatPoints.push([centerLat, centerLng, 1]);
        }
    });

    var heatLayer = L.heatLayer(heatPoints, {
        radius: 22,
        blur: 15,
        maxZoom: 12,
        max: 1,
        minOpacity: 0.45,
        gradient: { 0.2: '#1890ff', 0.5: '#faad14', 1.0: '#f5222d' }
    }).addTo(leafletMap);

    L.control.layers(baseLayers.baseMaps, {
        'Extents': extentsGroup,
        'Coverage heatmap': heatLayer
    }).addTo(leafletMap);

    document.getElementById(mapElement.id).style.width = "100%";

    if (extentsGroup.getLayers().length > 0) {
        leafletMap.fitBounds(extentsGroup.getBounds(), { padding: [12, 12] });
    } else {
        leafletMap.setView([0, 0], 1);
    }

    return leafletMap;
}
