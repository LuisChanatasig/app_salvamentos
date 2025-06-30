let map = null;
let markerSeleccionado = null;
let currentTileLayer = null;
let lugaresMarkers = [];
let latitudInicial = null;
let longitudInicial = null;

const userIcon = L.icon({
    iconUrl: 'https://cdn-icons-png.flaticon.com/512/684/684908.png',
    iconSize: [35, 35],
    iconAnchor: [17, 34],
    popupAnchor: [0, -30]
});

const lugarIcon = L.icon({
    iconUrl: 'https://cdn-icons-png.flaticon.com/512/854/854878.png',
    iconSize: [30, 30],
    iconAnchor: [15, 30],
    popupAnchor: [0, -25]
});

// Geolocalización inicial
function obtenerUbicacionUsuario() {
    if (navigator.geolocation) {
        navigator.geolocation.getCurrentPosition(
            position => {
                latitudInicial = position.coords.latitude;
                longitudInicial = position.coords.longitude;
            },
            () => {
                latitudInicial = -0.2299;
                longitudInicial = -78.5249;
            }
        );
    } else {
        latitudInicial = -0.2299;
        longitudInicial = -78.5249;
    }
}

function toggleMapa() {
    const mapContainer = document.getElementById('mapContainer');
    const toggleBtn = document.getElementById('toggleMapaBtn');

    if (mapContainer.classList.contains('fade-out') || mapContainer.style.display === 'none' || mapContainer.style.display === '') {
        mapContainer.style.display = 'block';
        requestAnimationFrame(() => {
            mapContainer.classList.remove('fade-out');
            mapContainer.classList.add('fade-in');
        });

        toggleBtn.innerHTML = '<i class="ri-map-pin-off-fill" style="font-size: 1.5rem;"></i>';

        if (!map) {
            map = L.map('map').setView([latitudInicial, longitudInicial], 14);
            setMapStyle('light');
            map.on('click', seleccionarUbicacionEnMapa);
            buscarLugares(latitudInicial, longitudInicial);
        }

    } else {
        mapContainer.classList.remove('fade-in');
        mapContainer.classList.add('fade-out');

        toggleBtn.innerHTML = '<i class="ri-map-pin-fill" style="font-size: 1.5rem;"></i>';

        setTimeout(() => { mapContainer.style.display = 'none'; }, 500);
    }
}

function seleccionarUbicacionEnMapa(e) {
    const lat = e.latlng.lat;
    const lon = e.latlng.lng;

    if (markerSeleccionado) map.removeLayer(markerSeleccionado);

    markerSeleccionado = L.marker([lat, lon], { icon: userIcon }).addTo(map).bindPopup('Ubicación seleccionada').openPopup();

    fetch(`https://nominatim.openstreetmap.org/reverse?format=json&lat=${lat}&lon=${lon}`)
        .then(res => res.json())
        .then(data => {
            const direccion = data.display_name || `Lat: ${lat.toFixed(6)}, Lon: ${lon.toFixed(6)}`;
            document.getElementById('direccionInput').value = direccion;
        });
}

// Cambiar estilos de mapa
function setMapStyle(style) {
    if (!map) return;

    if (currentTileLayer) map.removeLayer(currentTileLayer);

    let url, attribution;
    if (style === 'light') {
        url = 'https://{s}.basemaps.cartocdn.com/light_all/{z}/{x}/{y}{r}.png';
        attribution = '&copy; OpenStreetMap contributors &copy; CartoDB';
    } else if (style === 'dark') {
        url = 'https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png';
        attribution = '&copy; OpenStreetMap contributors &copy; CartoDB';
    } else if (style === 'satellite') {
        url = 'https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}';
        attribution = 'Tiles &copy; Esri';
    }

    currentTileLayer = L.tileLayer(url, { maxZoom: 19, attribution }).addTo(map);
}

// Buscar lugares (bares, parques, empresas, escuelas)
function buscarLugares(lat, lon) {
    const radius = 1000;
    const query = `
        [out:json];
        (
            node["amenity"="school"](around:${radius},${lat},${lon});
            node["amenity"="bar"](around:${radius},${lat},${lon});
            node["amenity"="cafe"](around:${radius},${lat},${lon});
            node["amenity"="restaurant"](around:${radius},${lat},${lon});
            node["amenity"="park"](around:${radius},${lat},${lon});
            node["amenity"="hospital"](around:${radius},${lat},${lon});
            node["office"](around:${radius},${lat},${lon});
        );
        out;
    `;

    fetch('https://overpass-api.de/api/interpreter', {
        method: 'POST',
        body: query
    })
        .then(res => res.json())
        .then(data => {
            limpiarLugaresAnteriores();

            data.elements.forEach(lugar => {
                const name = lugar.tags.name || 'Lugar sin nombre';
                const tipo = lugar.tags.amenity || lugar.tags.office || 'Sin tipo';

                const marcador = L.marker([lugar.lat, lugar.lon], { icon: lugarIcon })
                    .addTo(map)
                    .bindPopup(`<b>${name}</b><br>Tipo: ${tipo}`);

                lugaresMarkers.push(marcador);
            });
        });
}

function limpiarLugaresAnteriores() {
    lugaresMarkers.forEach(m => map.removeLayer(m));
    lugaresMarkers = [];
}

// Autocompletar direcciones
function debounce(func, delay) {
    let timer;
    return function (...args) {
        clearTimeout(timer);
        timer = setTimeout(() => func.apply(this, args), delay);
    };
}

const buscarDirecciones = debounce(() => {
    const query = document.getElementById('direccionInput').value.trim();
    const sugerenciasDiv = document.getElementById('sugerencias');

    if (query.length < 3) {
        sugerenciasDiv.style.display = 'none';
        return;
    }

    const apiURL = `https://nominatim.openstreetmap.org/search?format=json&q=${encodeURIComponent(query)}&addressdetails=1&limit=5`;

    fetch(apiURL)
        .then(res => res.json())
        .then(data => {
            if (!Array.isArray(data) || data.length === 0) {
                sugerenciasDiv.style.display = 'none';
                return;
            }

            sugerenciasDiv.innerHTML = '';
            data.forEach(item => {
                const div = document.createElement('div');
                div.textContent = item.display_name;
                div.style.padding = '5px';
                div.style.cursor = 'pointer';

                div.onclick = () => {
                    latitudSeleccionada = parseFloat(item.lat);
                    longitudSeleccionada = parseFloat(item.lon);

                    document.getElementById('direccionInput').value = item.display_name;
                    sugerenciasDiv.style.display = 'none';

                    if (map) {
                        map.setView([latitudSeleccionada, longitudSeleccionada], 16);

                        if (markerSeleccionado) map.removeLayer(markerSeleccionado);

                        markerSeleccionado = L.marker([latitudSeleccionada, longitudSeleccionada], { icon: userIcon })
                            .addTo(map)
                            .bindPopup('Ubicación seleccionada')
                            .openPopup();

                        buscarLugares(latitudSeleccionada, longitudSeleccionada);
                    }
                };

                sugerenciasDiv.appendChild(div);
            });

            sugerenciasDiv.style.display = 'block';
        });
}, 500);

document.getElementById('direccionInput').addEventListener('input', buscarDirecciones);

// Inicia con geolocalización
obtenerUbicacionUsuario();


// País que vas a consultar
const paisCiudad = 'Ecuador'; // Puedes poner cualquier país que tenga CitiesNow API

let listaCiudades = [];

// 1. Obtener el listado de ciudades al cargar la página o cuando se abra el campo
function obtenerCiudades() {
    fetch('https://countriesnow.space/api/v0.1/countries/cities', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ country: paisCiudad })
    })
        .then(res => res.json())
        .then(data => {
            if (data.error === false) {
                listaCiudades = data.data;
                console.log('Ciudades cargadas:', listaCiudades);
            } else {
                console.error('Error al obtener ciudades');
            }
        });
}

// 2. Mostrar sugerencias mientras escribes
function mostrarSugerenciasCiudades() {
    const input = document.getElementById('ciudadInput');
    const sugerenciasDiv = document.getElementById('sugerenciasCiudades');

    const query = input.value.trim().toLowerCase();

    if (query.length < 2) {
        sugerenciasDiv.style.display = 'none';
        return;
    }

    const coincidencias = listaCiudades.filter(ciudad => ciudad.toLowerCase().includes(query));

    sugerenciasDiv.innerHTML = '';

    if (coincidencias.length === 0) {
        sugerenciasDiv.style.display = 'none';
        return;
    }

    coincidencias.forEach(ciudad => {
        const div = document.createElement('div');
        div.textContent = ciudad;
        div.style.padding = '5px';
        div.style.cursor = 'pointer';

        div.onclick = () => {
            input.value = ciudad;
            sugerenciasDiv.style.display = 'none';
        };

        sugerenciasDiv.appendChild(div);
    });

    sugerenciasDiv.style.display = 'block';
}

// 3. Event listener para el input
document.getElementById('ciudadInput').addEventListener('input', mostrarSugerenciasCiudades);

// 4. Cargar ciudades al inicio
obtenerCiudades();
