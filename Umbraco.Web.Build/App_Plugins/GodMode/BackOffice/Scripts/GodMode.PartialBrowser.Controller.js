'use strict';
angular.module("umbraco").controller("GodMode.PartialBrowser.Controller",
    function ($scope, $routeParams, godModeResources, godModeConfig) {

        $scope.isLoading = true;
        $scope.config = godModeConfig.config;
        $scope.search = {};
        $scope.sort = {};
        $scope.sort.column = "Name";
        $scope.sort.reverse = false;
        $scope.triStateOptions = godModeResources.getTriStateOptions();
        $scope.search.isCached = $scope.triStateOptions[0];

        godModeResources.getTemplates().then(function (data) {
            $scope.templates = data;

            $scope.partials = data.map(function (p) {
                return p.Partials;
            }).reduce(function (a, b) {
                return a.concat(b);
            });

            $scope.isLoading = false;
        });

        $scope.sortBy = function (column) {
            $scope.sort.column = column;
            $scope.sort.reverse = !$scope.sort.reverse;
        }

        $scope.filterPartials = function (p) {

            if ($scope.search.partial) {
                if (p.Name.toLowerCase().indexOf($scope.search.partial.toLowerCase()) === -1) {
                    return;
                }
            }

            if ($scope.search.template) {
                return p.TemplateId === $scope.search.template.Id;
            }

            if ($scope.search.isCached.value) {
                return p.IsCached === $scope.search.isCached.value;
            }

            return p;
        };
    });