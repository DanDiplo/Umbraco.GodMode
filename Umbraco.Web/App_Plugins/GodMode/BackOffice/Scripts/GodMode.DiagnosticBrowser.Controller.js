(function () {
    'use strict';
    angular.module("umbraco").controller("GodMode.DiagnosticBrowser.Controller",
        function ($scope, $routeParams, $anchorScroll, $location, navigationService, godModeResources, godModeConfig) {

            $scope.isLoading = true;
            $scope.config = godModeConfig.config;
            $scope.search = {};
            $scope.sort = {};
            $scope.sort.column = "Name";
            $scope.sort.reverse = false;
            $scope.heading = "Diagnostics";

            godModeResources.getEnvironmentDiagnostics().then(function (data) {
                $scope.diagnostics = data;
                $scope.groups = $scope.diagnostics.map(function (g) {
                    return { Id: g.Id, Title: g.Title };
                });

                $scope.search.group = $scope.groups[0];
                $scope.selectGroup($scope.search.group);

                $scope.isLoading = false;
            });

            $scope.selectGroup = function (group) {

                var selectedGroup = $scope.diagnostics.filter(function (g) {
                    return g.Id === group.Id;
                });

                $scope.group = selectedGroup[0];
            }

            $scope.filterValues = function (v) {

                if ($scope.search.name && v.Key.toLowerCase().indexOf($scope.search.name.toLowerCase()) === -1) {
                    return;
                }

                if ($scope.search.value && v.Value != null && v.Value.toLowerCase().indexOf($scope.search.value.toLowerCase()) === -1) {
                    return;
                }

                return v;
            };

        });
})();