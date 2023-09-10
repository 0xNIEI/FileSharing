document.getElementById("download-button").addEventListener("click", function () {
    window.open("/download/?id="+document.getElementById("guid").innerHTML)
    var originalInnerHtml = this.innerHTML;
    this.innerHTML = '<i class="fa-solid fa-check"></i>';
    setTimeout(() => {
        this.innerHTML = originalInnerHtml;
    }, 2000);
});