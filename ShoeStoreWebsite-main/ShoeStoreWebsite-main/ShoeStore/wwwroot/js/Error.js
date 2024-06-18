document.addEventListener("DOMContentLoaded", function () {
    var errorAlert = document.getElementById("error-alert");
    if (errorAlert) {
        setTimeout(function () {
            errorAlert.style.display = "none";
        }, 3000);
    }
});