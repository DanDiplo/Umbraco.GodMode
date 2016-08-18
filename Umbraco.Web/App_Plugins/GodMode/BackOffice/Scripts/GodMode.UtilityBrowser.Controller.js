'use strict';
angular.module("umbraco").controller("GodMode.UtilityBrowser.Controller",
    function ($scope, $route, notificationsService, godModeResources, godModeConfig) {

        $scope.config = godModeConfig.config;

        var handleResponse = function (response) {

            if (response) {
                if (response.Response === "Error") {
                    notificationsService.error(response.Message);
                }
                else if (response.Response === "Success") {
                    notificationsService.success(response.Message);
                }
                if (response.Response === "Warning") {
                    notificationsService.warning(response.Message);
                }
            }
            else {
                notificationsService.error("No response was sent back from the server. Something didn't quite work out.");
            }
        }

        $scope.clearCache = function (cache) {

            godModeResources.clearUmbracoCache(cache).then(function (response) {
                handleResponse(response);
            });
        }

        $scope.restartAppPool = function () {

            if (window.confirm("This will take the site offline for a few moments. Are you sure?")) {

                godModeResources.restartAppPool().then(function (response) {
                    handleResponse(response);
                    document.location.reload(true);
                });
            }

        }

    });