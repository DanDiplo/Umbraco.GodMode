(function () {
    'use strict';
    angular.module("umbraco").controller("GodMode.UsageBrowser.Controller",
        function ($scope, $routeParams, navigationService, godModeResources, godModeConfig) {

            $scope.isLoading = true;
            $scope.config = godModeConfig.config;
            $scope.criteria = {};
            $scope.contentTypeAliases = [];
            $scope.sort = {};
            $scope.usage = [];

            $scope.fetchContent = function (orderBy) {

                orderBy = orderBy === undefined ? "CT.Alias" : orderBy;

                godModeResources.getContentUsage(null, orderBy).then(function (data) {
                    console.log(orderBy);
                    $scope.usage = data;
                    $scope.isLoading = false;
                });
            }

            $scope.sortBy = function (column) {
                $scope.sort.column = column;
                $scope.sort.reverse = !$scope.sort.reverse;
                var orderBy = $scope.sort.column;

                if (orderBy) {
                    orderBy = $scope.sort.reverse ? orderBy + " DESC" : orderBy + " ASC";
                };

                $scope.fetchContent(orderBy);
            };

            $scope.filterChange = function () {
                $scope.currentPage = 1;
                $scope.page = {};
                $scope.fetchContent();
            };

            $scope.fetchContent();
        });
})();