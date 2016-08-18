'use strict';
angular.module("umbraco").controller("GodMode.TemplateBrowser.Controller",
    function ($scope, $routeParams, godModeResources, godModeConfig) {

        $scope.isLoading = true;
        $scope.config = godModeConfig.config;
        $scope.search = {};

        godModeResources.getTemplates().then(function (data) {
            $scope.templates = data;
            $scope.partials = data.map(function (p) { return p.Partials; }).reduce(function (a, b) { return a.concat(b); })
            $scope.masters = data.filter(function (t) { return t.IsMaster; });
            $scope.isLoading = false;
        });

        $scope.filterTemplates = function (temp) {

            if ($scope.search.partial) {
                if (!temp.Partials.some(function (elem) {
                    return elem.Name === $scope.search.partial.Name;
                })) return;
            }

            if ($scope.search.master) {
                if (!temp.Parents.some(function (elem) {
                    return elem.Alias === $scope.search.master.Alias;
                })) return;
            }

            if ($scope.search.template) {
                if (temp.Name.toLowerCase().indexOf($scope.search.template.toLowerCase()) === -1 && temp.Alias.toLowerCase().indexOf($scope.search.template.toLowerCase()) === -1) {
                    return;
                }
            }

            return temp;
        };
    });