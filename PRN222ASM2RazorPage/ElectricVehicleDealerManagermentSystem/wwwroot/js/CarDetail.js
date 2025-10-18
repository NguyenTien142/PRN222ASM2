// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
    initializeMap();
    initializeActivityChart();
});

// Initialize Map with Leaflet
function initializeMap() {
    // Default coordinates (you can change this to any location)
    const latitude = -27.4698;
    const longitude = 153.0251;
    
    const map = L.map('map').setView([latitude, longitude], 13);
    
    // Add OpenStreetMap tiles
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
        maxZoom: 19
    }).addTo(map);
    
    // Add a marker
    const marker = L.marker([latitude, longitude]).addTo(map);
    marker.bindPopup('<b>Tesla Model X</b><br>Current Location').openPopup();
    
    // Custom marker icon (blue circle)
    const blueIcon = L.divIcon({
        className: 'custom-marker',
        html: '<div style="background-color: #007aff; width: 20px; height: 20px; border-radius: 50%; border: 3px solid white; box-shadow: 0 2px 6px rgba(0,0,0,0.3);"></div>',
        iconSize: [20, 20],
        iconAnchor: [10, 10]
    });
    
    marker.setIcon(blueIcon);
}

// Initialize Activity Chart with Chart.js
function initializeActivityChart() {
    const ctx = document.getElementById('activityChart');
    
    if (!ctx) {
        console.error('Activity chart canvas not found');
        return;
    }
    
    // Sample data for the activity chart
    const data = {
        labels: ['12/09', '12/09', '12/09', '12/09', '12/09', '12/09', '12/09'],
        datasets: [{
            label: 'Distance',
            data: [35, 55, 75, 50, 45, 60, 40],
            backgroundColor: 'rgba(0, 122, 255, 0.6)',
            borderColor: 'rgba(0, 122, 255, 1)',
            borderWidth: 0,
            borderRadius: 4,
            barThickness: 20
        }]
    };
    
    const config = {
        type: 'bar',
        data: data,
        options: {
            responsive: true,
            maintainAspectRatio: true,
            plugins: {
                legend: {
                    display: false
                },
                tooltip: {
                    backgroundColor: 'rgba(0, 0, 0, 0.8)',
                    padding: 12,
                    cornerRadius: 8,
                    titleColor: '#fff',
                    bodyColor: '#fff',
                    callbacks: {
                        label: function(context) {
                            return context.parsed.y + 'km';
                        }
                    }
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    max: 100,
                    ticks: {
                        callback: function(value) {
                            return value + 'km';
                        },
                        color: '#86868b',
                        font: {
                            size: 11
                        }
                    },
                    grid: {
                        color: '#e5e5e7',
                        drawBorder: false
                    }
                },
                x: {
                    ticks: {
                        color: '#86868b',
                        font: {
                            size: 11
                        }
                    },
                    grid: {
                        display: false,
                        drawBorder: false
                    }
                }
            }
        }
    };
    
    new Chart(ctx, config);
}

// Optional: Add animation to progress bars
function animateProgressBars() {
    const progressBars = document.querySelectorAll('.progress-bar');
    progressBars.forEach(bar => {
        const width = bar.style.width;
        bar.style.width = '0%';
        setTimeout(() => {
            bar.style.transition = 'width 1s ease-in-out';
            bar.style.width = width;
        }, 100);
    });
}

// Call animation after page load
window.addEventListener('load', animateProgressBars);

// Optional: Add interactive features
document.querySelectorAll('.view-all-link').forEach(link => {
    link.addEventListener('click', function(e) {
        e.preventDefault();
        console.log('View All clicked:', this.previousElementSibling.textContent);
        // Add your custom logic here
    });
});

// Optional: Add button functionality
const addButton = document.querySelector('.btn-outline-secondary');
if (addButton) {
    addButton.addEventListener('click', function() {
        alert('Add new reminder functionality - implement as needed');
        // Add your custom logic here
    });
}
