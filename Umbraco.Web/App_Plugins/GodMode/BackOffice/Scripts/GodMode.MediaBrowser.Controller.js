'use strict'
angular.module("umbraco").controller("GodMode.MediaBrowser.Controller",
    function ($scope, $routeParams, godModeResources, godModeConfig) {

        $scope.isLoading = true;
        $scope.config = godModeConfig.config;
        $scope.media = [];
        $scope.search = {};
        $scope.sort = {};
        $scope.sort.column = "Name";
        $scope.sort.reverse = false;

        $scope.fileSizes = [
            { "min": 0, "max": 10240, "title": "Under 10KB" },
            { "min": 10241, "max": 51200, "title": "10KB - 50KB" },
            { "min": 51201, "max": 102400, "title": "50KB - 100KB" },
            { "min": 102401, "max": 204800, "title": "100KB - 200KB" },
            { "min": 204801, "max": 512000, "title": "200KB - 500KB" },
            { "min": 512000, "max": 1024000, "title": "500KB - 1MB" },
            { "min": 1024001, "max": 2048000, "title": "1MB - 2MB" },
            { "min": 2048001, "max": 5120000, "title": "2MB - 5MB" },
            { "min": 5120001, "max": 10240000, "title": "5MB - 10MB" },
            { "min": 10240001, "max": 9007199254740991, "title": "Over 10MB" },
        ];

        godModeResources.getMedia().then(function (data) {
            $scope.media = data;
            $scope.isLoading = false;
        });

        $scope.sortBy = function (column) {
            $scope.sort.column = column;
            $scope.sort.reverse = !$scope.sort.reverse;
        }

        $scope.filterMedia = function (m) {

            if ($scope.search.id) {
                if (m.Id.toString() !== $scope.search.id) {
                    return;
                }
            }

            if ($scope.search.name) {
                if (m.Name.toLowerCase().indexOf($scope.search.name.toLowerCase()) === -1) {
                    return;
                }
            }

            if ($scope.search.mediaType) {
                if (m.Alias !== $scope.search.mediaType.Alias) {
                    return;
                }
            }

            if ($scope.search.fileType) {
                if (m.Type !== $scope.search.fileType.Type) {
                    return;
                }
            }

            if ($scope.search.fileSize) {
                if (!(parseInt(m.Size) > $scope.search.fileSize.min && parseInt(m.Size) <= $scope.search.fileSize.max)) {
                    return;
                }
            }

            return m;
        };
    });