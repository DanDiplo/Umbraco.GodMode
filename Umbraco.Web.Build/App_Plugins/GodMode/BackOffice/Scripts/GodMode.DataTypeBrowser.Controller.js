'use strict';
angular.module("umbraco").controller("GodMode.DataTypeBrowser.Controller",
    function ($scope, $routeParams, godModeResources, godModeConfig) {

        $scope.isLoading = true;
        $scope.config = godModeConfig.config;
        $scope.search = {};
        $scope.sort = {};
        $scope.sort.column = "Name";
        $scope.sort.reverse = false;

        $scope.triStateOptions = godModeResources.getTriStateOptions();
        $scope.search.isUsed = $scope.triStateOptions[0];

        godModeResources.getDataTypesStatus().then(function (data) {
            $scope.dataTypes = data;
            $scope.isLoading = false;
        });

        $scope.sortBy = function (column) {
            $scope.sort.column = column;
            $scope.sort.reverse = !$scope.sort.reverse;
        }

        godModeResources.getPropertyEditors().then(function (data) {
            $scope.propertyEditors = data;
        });

        $scope.filterDataTypes = function (d) {

            if ($scope.search.dataType) {
                if (d.Name.toLowerCase().indexOf($scope.search.dataType.toLowerCase()) === -1) {
                    return;
                }
            }

            if ($scope.search.propertyEditor) {
                if (d.Alias.toLowerCase().indexOf($scope.search.propertyEditor.Alias.toLowerCase()) === -1) {
                    return;
                }
            }

            if ($scope.search.dbType) {
                if (d.DbType !== $scope.search.dbType.DbType) {
                    return;
                }
            }

            if (!d.IsUsed === $scope.search.isUsed.value) {
                return;
            }

            return d;
        };
    });