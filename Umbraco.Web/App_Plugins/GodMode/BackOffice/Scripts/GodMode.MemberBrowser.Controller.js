(function () {
    'use strict';
    angular.module("umbraco").controller("GodMode.MemberBrowser.Controller",
        function ($scope, $routeParams, navigationService, godModeResources, godModeConfig) {

            $scope.isLoading = true;
            $scope.config = godModeConfig.config;
            $scope.criteria = {
                group: { Name: "", Id: null },
                search: null
            };
            $scope.page = {};
            $scope.currentPage = 1;
            $scope.itemsPerPage = 15;
            $scope.members = [];
            $scope.memberGroups = [];
            $scope.sort = {};

            $scope.fetchMembers = function (orderBy) {

                $scope.isLoading = true;
                var groupId = $scope.criteria.group != null ? $scope.criteria.group.Id : null;

                godModeResources.getMembersPaged($scope.currentPage, $scope.itemsPerPage, groupId, $scope.criteria.search, orderBy).then(function (data) {
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

                $scope.fetchMembers(orderBy);
            }

            $scope.prevPage = function () {
                if ($scope.currentPage > 1) {
                    $scope.currentPage--;
                    $scope.fetchMembers();
                }
            };

            $scope.nextPage = function () {
                if ($scope.currentPage < $scope.page.TotalPages) {
                    $scope.currentPage++;
                    $scope.fetchMembers();
                }
            };

            $scope.setPage = function (pageNumber) {
                $scope.currentPage = pageNumber;
                $scope.fetchMembers();
            };

            $scope.filterChange = function () {
                $scope.currentPage = 1;
                $scope.page = {};
                $scope.fetchMembers();
            };

            godModeResources.getMemberGroups().then(function (data) {
                $scope.memberGroups = data;
                $scope.fetchMembers();
            });
        });
})();