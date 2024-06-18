document.addEventListener("DOMContentLoaded", function () {
        var successAlert = document.getElementById("success-alert");
    if (successAlert) {
        setTimeout(function () {
            successAlert.style.display = "none";
        }, 3000);

    }
});
