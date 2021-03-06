/// <autosync enabled="true" />
/// <reference path="modernizr-2.6.2.js" />
/// <reference path="jquery-1.10.2.js" />
/// <reference path="bootstrap.js" />
/// <reference path="respond.js" />
/// <reference path="jquery.validate.js" />
/// <reference path="jquery.validate.unobtrusive.js" />
/// <reference path="jquery.unobtrusive-ajax.js" />
var selectedProductID;
var Fromdate;
var Todate;

$("#ProductId").change(function () {

    selectedProductID = $("#lstProduct").id().trim();

    if (selectedProductID != -1)
    {
        getProductTable();
        $("#ProjectSection").fadeIn();
    }
});

function getProductTable() {
    $.ajax({
        
        url: "/Order/DisplayProductPrice",
        type: 'Get',
        data:
            {
                ProductId: selectedProductID
            },
        success: function (data) {
            $("#txtUnitPrice").empty().append(data);
        },
        error: function () {
            alert("something seems wrong");
        }
    });
}