(function () {
'use strict';
angular.module("umbraco")
    .constant("godModeConfig", {
        "baseApiUrl": "BackOffice/Api/GodModeApi/",
        "config": {
            "version": "2.0.0",
            "editTemplateUrl": "#/settings/templates/edit/",
            "editDataTypeUrl": "#/settings/datatypes/edit/",
            "editDocTypeUrl": "#settings/documentTypes/edit/",
            "editPartialUrl": "#/settings/partialViews/edit/",
            "editMediaUrl": "#/media/media/edit/",
            "editContentUrl": "#/content/content/edit/",
            "baseTreeUrl": "#/settings/godModeTree/",
            "baseViewUrl": "#/settings/godModeTree/"
        }
    })
    .directive('godmodeTrueFalse', function () {
        return {
            restrict: 'E',
            scope: {
                value: '='
            },
            link: function (scope, element, attrs) {
            },
            template: '<span ng-show="value"><i class="icon icon-checkbox"></i> Yes</span><span ng-show="!value"><i class="icon icon-checkbox-empty"></i> No</span>'
        };
    })
    .directive('godmodeSortable', function () {
        return {
            restrict: 'E',
            scope: {
                column: '@',
                sort: "=",
                sortBy: "&"
            },
            transclude: true,
            link: function (scope, element, attrs) {
            },
            template: '<a ng-click="sortBy()" ng-transclude></a><i class="icon" ng-show="sort.column === column && !sort.reverse">&#9650;</i><i class="icon" ng-show="sort.column === column && sort.reverse">&#9660;</i>'
        };
    })
    .directive('godmodeHeader', function ($route, godModeConfig) {
        return {
            restrict: 'E',
            scope: {
                heading: '@',
                tooltip: '@'
            },
            link: function (scope, element, attrs) {
                scope.version = godModeConfig.config.version;
                scope.reload = function () {
                    $route.reload();
                };
            },
            templateUrl: "/App_Plugins/DiploGodMode/BackOffice/GodModeTree/godModeHeader.html"
        };
    })
    .filter('godModeFileSize', function () {
        return function (bytes, precision) {
            if (!bytes) return "N/A";
            if (isNaN(parseFloat(bytes)) || !isFinite(bytes)) return '-';
            if (typeof precision === 'undefined') precision = 1;
            var units = ['bytes', 'KB', 'MB', 'GB'];
            var number = Math.floor(Math.log(bytes) / Math.log(1024));
            return (bytes / Math.pow(1024, Math.floor(number))).toFixed(precision) + ' ' + units[number];
        };
    })
    .filter('godModeUnique', function () {

        return function (items, filterOn) {

            if (filterOn === false) {
                return items;
            }

            if ((filterOn || angular.isUndefined(filterOn)) && angular.isArray(items)) {
                var hashCheck = {}, newItems = [];

                var extractValueToCompare = function (item) {
                    if (angular.isObject(item) && angular.isString(filterOn)) {
                        return item[filterOn];
                    } else {
                        return item;
                    }
                };

                angular.forEach(items, function (item) {
                    var valueToCheck, isDuplicate = false;

                    for (var i = 0; i < newItems.length; i++) {
                        if (angular.equals(extractValueToCompare(newItems[i]), extractValueToCompare(item))) {
                            isDuplicate = true;
                            break;
                        }
                    }
                    if (!isDuplicate) {
                        newItems.push(item);
                    }

                });
                items = newItems;
            }
            return items;
        };
    });
})();