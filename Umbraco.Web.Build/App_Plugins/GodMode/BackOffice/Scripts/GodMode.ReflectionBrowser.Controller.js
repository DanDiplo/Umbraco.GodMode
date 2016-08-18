'use strict';
angular.module("umbraco").controller("GodMode.ReflectionBrowser.Controller",
    function ($scope, $routeParams, godModeResources, godModeConfig) {

        $scope.isLoading = true;
        $scope.config = godModeConfig.config;
        $scope.search = {};
        $scope.sort = {};
        $scope.sort.column = "Name";
        $scope.sort.reverse = false;
        $scope.triStateOptions = godModeResources.getTriStateOptions();
        $scope.search.isUmbraco = $scope.triStateOptions[2];
        $scope.heading = "";

        var id = $routeParams.id;
        var getControllersFunction;

        if ($routeParams.id === "api") {
            getControllersFunction = godModeResources.getApiControllers();
            $scope.heading = "API Controllers";
        }
        else if ($routeParams.id === "surface") {
            getControllersFunction = godModeResources.getSurfaceControllers()
            $scope.heading = "Surface Controllers";
        }
        else if ($routeParams.id === "models") {
            getControllersFunction = godModeResources.getPublishedContentModels()
            $scope.heading = "Published Content Models";
        }
        else if ($routeParams.id === "render") {
            getControllersFunction = godModeResources.getRenderMvcControllers()
            $scope.heading = "RenderMvc Controllers";
        }
        else if ($routeParams.id === "converters") {
            getControllersFunction = godModeResources.getPropertyValueConveters()
            $scope.heading = "Property Value Converters";
        }

        getControllersFunction.then(function (data) {
            $scope.controllers = data;
            $scope.isLoading = false;
        });

        $scope.filterByUmbraco = function (c) {
            if (!c.IsUmbraco === $scope.search.isUmbraco.value) {
                return;
            }
            return c;
        }

        $scope.sortBy = function (column) {
            $scope.sort.column = column;
            $scope.sort.reverse = !$scope.sort.reverse;
        }

        $scope.resetFilters = function () {
            $scope.search.namespace = null;
            $scope.search.baseType = null;
        }

        $scope.filterControllers = function (c) {

            if ($scope.search.controller && c.Name.toLowerCase().indexOf($scope.search.controller.toLowerCase()) === -1) {
                return;
            }

            if (!c.IsUmbraco === $scope.search.isUmbraco.value) {
                return;
            }

            if ($scope.search.namespace && c.Namespace !== $scope.search.namespace.Namespace) {
                return;
            }

            if ($scope.search.baseType && c.BaseType !== $scope.search.baseType.BaseType) {

                return;
            }

            return c;
        };
    });