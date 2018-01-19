(function () {
    'use strict';
    angular.module("umbraco").controller("GodMode.ContentBrowser.Controller",
        function ($scope, $routeParams, navigationService, godModeResources, godModeConfig) {

            $scope.isLoading = true;
            $scope.config = godModeConfig.config;
            $scope.criteria = {};
            $scope.page = {};
            $scope.currentPage = 1;
            $scope.itemsPerPage = 15;
            $scope.content = [];
            $scope.contentTypeAliases = [];
            $scope.sort = {};
            $scope.triStateOptions = godModeResources.getTriStateOptions();
            $scope.criteria.Trashed = $scope.triStateOptions[0];
            var param = $routeParams.id;

            $scope.fetchContent = function (orderBy) {
                godModeResources.getContentPaged($scope.currentPage, $scope.itemsPerPage, $scope.criteria, orderBy).then(function (data) {
                    $scope.page = data;
                    $scope.currentPage = $scope.page.CurrentPage;
                    $scope.isLoading = false;
                });
            }

            $scope.sortBy = function (column) {
                $scope.sort.column = column;
                $scope.sort.reverse = !$scope.sort.reverse;
                var orderBy = $scope.sort.column;

                if (orderBy) {
                    orderBy = $scope.sort.reverse ? orderBy + " DESC" : orderBy + " ASC";
                }

                $scope.fetchContent(orderBy);
            }

            $scope.prevPage = function () {
                if ($scope.currentPage > 1) {
                    $scope.currentPage--;
                    $scope.fetchContent();
                }
            };

            $scope.nextPage = function () {
                if ($scope.currentPage < $scope.page.TotalPages) {
                    $scope.currentPage++;
                    $scope.fetchContent();
                }
            };

            $scope.setPage = function (pageNumber) {
                $scope.currentPage = pageNumber;
                $scope.fetchContent();
            };

            $scope.filterChange = function () {
                $scope.currentPage = 1;
                $scope.page = {};
                $scope.fetchContent();
            };

            godModeResources.getContentTypeAliases().then(function (data) {
                $scope.contentTypeAliases = data;

                if (param != "browse") {
                    $scope.contentTypeAliases.filter(function (elem) {
                        if (elem == param) {
                            $scope.criteria.Alias = elem;
                        }
                    });
                };

                $scope.fetchContent();
            });
        });
})();