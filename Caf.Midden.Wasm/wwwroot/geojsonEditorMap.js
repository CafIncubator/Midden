import { createBaseLayers } from './geojsonMap.js';

// Default view when no geometry is set: the continental US, which matches the USGS basemaps
const defaultCenter = [39.5, -98.35];
const defaultZoom = 4;
const coordinatePrecision = 8;

// Rounds every coordinate value in a (possibly deeply nested) coordinates array
function roundCoordinates(coordinates) {
    if (typeof coordinates === 'number') {
        return Number(coordinates.toFixed(coordinatePrecision));
    }

    if (Array.isArray(coordinates)) {
        return coordinates.map(roundCoordinates);
    }

    return coordinates;
}

// Parses a bare geojson geometry string, returning null when it isn't usable
function parseGeometry(geometryJson) {
    if (!geometryJson || geometryJson.trim() === '') {
        return null;
    }

    try {
        var parsed = JSON.parse(geometryJson);

        if (!parsed || !parsed.type || !parsed.coordinates) {
            return null;
        }

        return parsed;
    } catch {
        return null;
    }
}

// Removes the current shape, without notifying .NET
function clearLayer(state) {
    if (state.layer) {
        state.drawnItems.removeLayer(state.layer);
        state.layer = null;
    }
}

// Serializes the current shape as a bare geojson geometry and pushes it back to .NET
function notifyGeometryChanged(state) {
    var geometry = '';

    if (state.layer) {
        var featureJson = state.layer.toGeoJSON();
        var layerGeometry = featureJson.geometry ?? featureJson;
        layerGeometry.coordinates = roundCoordinates(layerGeometry.coordinates);
        geometry = JSON.stringify(layerGeometry);
    }

    state.lastPushedGeometry = geometry;
    state.dotNetRef.invokeMethodAsync('OnGeometryChangedFromMap', geometry);
}

// Wires up the edit/drag/vertex events that report changes back to .NET
function attachLayerHandlers(state, layer) {
    var events = [
        'pm:edit',
        'pm:update',
        'pm:dragend',
        'pm:markerdragend',
        'pm:vertexadded',
        'pm:vertexremoved'
    ];

    events.forEach(eventName => layer.on(eventName, () => notifyGeometryChanged(state)));
}

// Adds a shape built from a geojson geometry, enforcing the single-shape rule
function addGeometryLayer(state, geometry) {
    clearLayer(state);

    var geoJsonLayer = L.geoJSON({
        "type": "Feature",
        "properties": {},
        "geometry": geometry
    });

    var layers = geoJsonLayer.getLayers();

    if (layers.length === 0) {
        return null;
    }

    var layer = layers[0];
    state.drawnItems.addLayer(layer);
    state.layer = layer;
    attachLayerHandlers(state, layer);

    return layer;
}

// Zooms to the current shape, or to the default view when there isn't one
function zoomToLayer(state) {
    if (!state.layer) {
        state.map.setView(defaultCenter, defaultZoom);
        return;
    }

    if (typeof state.layer.getBounds === 'function') {
        var bounds = state.layer.getBounds();

        if (bounds.isValid()) {
            state.map.fitBounds(bounds, { padding: [20, 20] });
            return;
        }
    }

    if (typeof state.layer.getLatLng === 'function') {
        state.map.setView(state.layer.getLatLng(), 12);
        return;
    }

    state.map.setView(defaultCenter, defaultZoom);
}

// Adds a toolbar button that asks .NET to show the raw geojson dialog
function addRawGeoJsonControl(state) {
    var RawGeoJsonControl = L.Control.extend({
        options: { position: 'topright' },
        onAdd: function () {
            var container = L.DomUtil.create('div', 'leaflet-bar leaflet-control geojson-raw-control');
            var link = L.DomUtil.create('a', '', container);
            link.href = '#';
            link.title = 'View/edit raw GeoJSON';
            link.innerHTML = '{ }';

            L.DomEvent.disableClickPropagation(container);
            L.DomEvent.on(link, 'click', L.DomEvent.stop);
            L.DomEvent.on(link, 'click', () => state.dotNetRef.invokeMethodAsync('ShowRawGeoJson'));

            return container;
        }
    });

    state.map.addControl(new RawGeoJsonControl());
}

// Creates the editable leaflet map and returns the state handle used by the other functions
export function create(mapElement, geometryJson, dotNetRef, readOnly) {
    var baseLayers = createBaseLayers();

    var map = L.map(mapElement, { layers: baseLayers.layers });

    L.control.layers(baseLayers.baseMaps).addTo(map);

    var state = {
        map: map,
        drawnItems: L.featureGroup().addTo(map),
        layer: null,
        dotNetRef: dotNetRef,
        lastPushedGeometry: null,
        resizeObserver: null
    };

    mapElement.style.width = "100%";

    var geometry = parseGeometry(geometryJson);

    if (geometry) {
        addGeometryLayer(state, geometry);
    }

    zoomToLayer(state);

    if (!readOnly) {
        map.pm.addControls({
            position: 'topleft',
            drawMarker: true,
            drawPolygon: true,
            drawRectangle: true,
            drawCircle: false,
            drawCircleMarker: false,
            drawPolyline: false,
            drawText: false,
            editMode: true,
            dragMode: true,
            removalMode: true,
            cutPolygon: false,
            rotateMode: false
        });

        // A dataset has a single spatial extent, so a newly drawn shape replaces the previous one.
        // Capture the previous geometry first so .NET can offer an undo.
        map.on('pm:create', e => {
            var previousGeometry = '';

            if (state.layer) {
                var prevFeature = state.layer.toGeoJSON();
                var prevGeom = prevFeature.geometry ?? prevFeature;
                prevGeom.coordinates = roundCoordinates(prevGeom.coordinates);
                previousGeometry = JSON.stringify(prevGeom);
            }

            map.removeLayer(e.layer);
            clearLayer(state);

            state.drawnItems.addLayer(e.layer);
            state.layer = e.layer;
            attachLayerHandlers(state, e.layer);

            notifyGeometryChanged(state);

            // Only notify about a replacement when there was an existing shape
            if (previousGeometry) {
                state.dotNetRef.invokeMethodAsync('OnShapeReplaced', previousGeometry);
            }
        });

        map.on('pm:remove', () => {
            state.layer = null;
            notifyGeometryChanged(state);
        });
    }

    addRawGeoJsonControl(state);

    // Keeps the map sized correctly when it is revealed inside a tab or a resizing container
    if (typeof ResizeObserver !== 'undefined') {
        state.resizeObserver = new ResizeObserver(() => map.invalidateSize());
        state.resizeObserver.observe(mapElement);
    }

    return state;
}

// Replaces the displayed shape with the supplied geometry. Returns false when it isn't valid geojson.
export function setGeometry(state, geometryJson) {
    if (!state || !state.map) {
        return false;
    }

    // Ignores the value that the map itself just produced
    if (geometryJson === state.lastPushedGeometry) {
        return true;
    }

    if (!geometryJson || geometryJson.trim() === '') {
        clearLayer(state);
        zoomToLayer(state);
        state.lastPushedGeometry = geometryJson;
        return true;
    }

    var geometry = parseGeometry(geometryJson);

    if (!geometry) {
        // Keeps the last valid shape on the map
        return false;
    }

    var layer = addGeometryLayer(state, geometry);

    if (!layer) {
        return false;
    }

    zoomToLayer(state);
    state.lastPushedGeometry = geometryJson;

    return true;
}

// Recalculates the map size, needed when the map becomes visible inside a hidden tab
export function invalidateSize(state) {
    if (state && state.map) {
        state.map.invalidateSize();
    }
}

export function dispose(state) {
    if (!state) {
        return;
    }

    if (state.resizeObserver) {
        state.resizeObserver.disconnect();
        state.resizeObserver = null;
    }

    if (state.map) {
        state.map.remove();
        state.map = null;
    }

    state.layer = null;
    state.dotNetRef = null;
}
