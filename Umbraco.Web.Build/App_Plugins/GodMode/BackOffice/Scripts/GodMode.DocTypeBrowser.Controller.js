'use strict';
angular.module("umbraco").controller("GodMode.DocTypeBrowser.Controller",
    function ($scope, $routeParams, $route, godModeResources, godModeConfig) {

        $scope.isLoading = true;
        $scope.config = godModeConfig.config;
        $scope.search = {};
        $scope.search.includeInherited = true.toString();
        var param = $routeParams.id;

        $scope.triStateOptions = godModeResources.getTriStateOptions();

        $scope.search.hasTemplate = $scope.triStateOptions[0];
        $scope.search.isListView = $scope.triStateOptions[0];
        $scope.search.allowedAtRoot = $scope.triStateOptions[0];
        $scope.search.hasCompositions = $scope.triStateOptions[0];

        godModeResources.getContentTypeMap().then(function (data) {
            $scope.contentTypes = data;
            $scope.isLoading = false;
        });

        godModeResources.getPropertyGroups().then(function (data) {
            $scope.propertyGroups = data;
        });

        godModeResources.getCompositions().then(function (data) {
            $scope.compositions = data;
        });

        godModeResources.getDataTypes().then(function (data) {
            $scope.dataTypes = data;
        });

        godModeResources.getPropertyEditors().then(function (data) {
            $scope.propertyEditors = data;
            if (param != "browse") {
                $scope.propertyEditors.filter(function (elem) {
                    if (elem.Alias == param) {
                        $scope.search.propertyEditor = elem;
                    }
                });
            }
        });

        $scope.filterContentTypes = function (ct) {

            if ($scope.search.doctype) {
                if (ct.Name.toLowerCase().indexOf($scope.search.doctype.toLowerCase()) === -1 && ct.Alias.toLowerCase().indexOf($scope.search.doctype.toLowerCase()) === -1) {
                    return;
                }
            }

            if (!ct.HasTemplates === $scope.search.hasTemplate.value) {
                return;
            }

            if (!ct.IsListView === $scope.search.isListView.value) {
                return;
            }

            if (!ct.AllowedAtRoot === $scope.search.allowedAtRoot.value) {
                return;
            }

            if (!ct.HasCompositions === $scope.search.hasCompositions.value) {
                return;
            }

            if ($scope.search.propertyGroup && ct.PropertyGroups.indexOf($scope.search.propertyGroup) === -1) {
                return;
            }

            if ($scope.search.composedOf) {
                if (!ct.Compositions.some(function (elem) {
                    return elem.Id === $scope.search.composedOf.Id;
                })) return;
            }

            if ($scope.search.property) {
                var props = $scope.search.includeInherited === true.toString() ? ct.AllProperties : ct.Properties;
                if (!props.some(function (elem) {
                    return elem.Alias.toLowerCase().indexOf($scope.search.property.toLowerCase()) > -1 && elem.Name.toLowerCase().indexOf($scope.search.property.toLowerCase()) > -1;
                })) return;
            }

            if ($scope.search.template) {
                if (!ct.Templates.some(function (elem) {
                    return elem.Alias.toLowerCase().indexOf($scope.search.template.toLowerCase()) > -1 && elem.Name.toLowerCase().indexOf($scope.search.template.toLowerCase()) > -1;
                })) return;
            }

            if ($scope.search.dataType) {
                if (!ct.AllProperties.some(function (elem) {
                    return elem.EditorId === $scope.search.dataType.Id;
                })) return;
            }

            if ($scope.search.propertyEditor) {
                if (!ct.AllProperties.some(function (elem) {
                    return elem.EditorAlias === $scope.search.propertyEditor.Alias;
                })) return;
            }

            return ct;
        }

    });