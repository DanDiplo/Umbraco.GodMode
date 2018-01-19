(function () {
    'use strict';
    angular.module("umbraco").controller("GodMode.UtilityBrowser.Controller",
        function ($scope, $route, $http, navigationService, notificationsService, godModeResources, godModeConfig) {

            $scope.config = godModeConfig.config;
            $scope.warmup = {
                current: 0,
                count: 0,
                url: null
            };

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

            $scope.warmUpTemplates = function () {
                godModeResources.getTemplateUrls().then(function (response) {
                    $scope.warmup.current = 0;
                    $scope.warmup.url = "[waiting...]";

                    if (!response) {
                        console.log("Error fetching URLs....");
                    }

                    $scope.warmup.count = response.length;

                    angular.forEach(response, function (url) {
                        $http.get(url).then(function (res) {
                            $scope.warmup.current++;
                            $scope.warmup.url = url;
                            console.log("Warmed up page: " + url);
                        }, function (err) {
                            $scope.warmup.current++;
                            $scope.warmup.url = url;
                        });
                    });
                });
            }
        });
})();