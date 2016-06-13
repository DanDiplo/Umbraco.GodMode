'use strict'
angular.module("umbraco").controller("GodMode.DiagnosticBrowser.Controller",
    function ($scope, $routeParams, $anchorScroll, $location, godModeResources, godModeConfig) {



        $scope.isLoading = true;
        $scope.config = godModeConfig.config;
        $scope.search = {};
        $scope.sort = {};
        $scope.sort.column = "Name";
        $scope.sort.reverse = false;

        godModeResources.getEnvironmentDiagnostics().then(function (data) {
            $scope.diagnostics = data;
            var i = 0;
            $scope.sections = data.map(function (x) {
                return { id: "section" + i++,  heading: x.Heading };
            });

            $scope.isLoading = false;
        });

        $scope.filterValues = function (v) {

            if ($scope.search.name && v.Key.toLowerCase().indexOf($scope.search.name.toLowerCase()) === -1) {
                return;
            }

            if ($scope.search.value && v.Value.toLowerCase().indexOf($scope.search.value.toLowerCase()) === -1) {
                return;
            }

            return v;
        };

        $scope.scrollTo = function (id) {

            $scope.search.name = $scope.search.value = null;

            $location.hash(id);
            $anchorScroll.yOffset = 200;
            $anchorScroll();
        }

    });