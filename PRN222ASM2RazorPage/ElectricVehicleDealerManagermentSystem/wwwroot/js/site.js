if (typeof window.signalRSetupInitialized === "undefined") {
    window.signalRSetupInitialized = true;

    const currentPath = window.location.pathname.toLowerCase();
    const isOnCategoryIndex = currentPath === "/category/index" || currentPath === "/category";
    const isOnVehicleIndex = currentPath === "/vehicle/index" || currentPath === "/vehicle";
    const isOnVehicleBrowse = currentPath === "/vehicle/browse";
    const isOnInventoryIndex = currentPath === "/inventory" || currentPath === "/inventory/index";
    const isOnAppointmentManage = currentPath === "/appointment/manage";
    const isOnAppointmentIndex = currentPath === "/appointment" || currentPath === "/appointment/index";
    const isOnDealerOrders = currentPath === "/dealer/orders";
    const isOnCustomerOrders = currentPath === "/customer/myorders";

    if (isOnCategoryIndex || isOnVehicleIndex || isOnVehicleBrowse || isOnInventoryIndex || isOnAppointmentManage || isOnAppointmentIndex || isOnDealerOrders || isOnCustomerOrders) {
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
                    console.log("SignalR connected for realtime updates on", currentPath);
                })
                .catch(function (err) {
                    console.error("SignalR connection error:", err.toString());
                });
        }
    }
}