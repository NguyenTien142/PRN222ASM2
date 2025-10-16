if (typeof window.signalRSetupInitialized === "undefined") {
    window.signalRSetupInitialized = true;

    const currentPath = window.location.pathname.toLowerCase();
    const isOnCategoryIndex = currentPath === "/category/index" || currentPath === "/category" || currentPath === "/categories";

    if (isOnCategoryIndex) {
        if (typeof signalR === 'undefined' || !signalR.HubConnectionBuilder) {
            console.warn('SignalR client not loaded');
        } else {
            const connection = new signalR.HubConnectionBuilder()
                .withUrl("/signalRHub")
                .withAutomaticReconnect()
                .build();

            connection.on("LoadAllItems", function () {
                // Reload the page or fetch fresh data
                location.reload();
            });

            connection.start()
                .then(function () {
                    console.log("SignalR connected for Category Index");
                })
                .catch(function (err) {
                    console.error("SignalR connection error:", err.toString());
                });
        }
    }
}