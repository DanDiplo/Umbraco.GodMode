'use strict'
angular.module('umbraco.resources').factory('godModeResources', function ($q, $http, umbRequestHelper, godModeConfig) {
    return {
        getContentTypeMap: function () {
            return umbRequestHelper.resourcePromise(
                $http.get(godModeConfig.baseApiUrl + "GetContentTypeMap")
            );
        },
        getPropertyGroups: function () {
            return umbRequestHelper.resourcePromise(
                $http.get(godModeConfig.baseApiUrl + "GetPropertyGroups")
            );
        },
        getCompositions: function () {
            return umbRequestHelper.resourcePromise(
                $http.get(godModeConfig.baseApiUrl + "GetCompositions")
            );
        },
        getDataTypes: function () {
            return umbRequestHelper.resourcePromise(
                $http.get(godModeConfig.baseApiUrl + "GetDataTypes")
            );
        },
        getDataTypesStatus: function () {
            return umbRequestHelper.resourcePromise(
                $http.get(godModeConfig.baseApiUrl + "GetDataTypesStatus")
            );
        },
        getPropertyEditors: function () {
            return umbRequestHelper.resourcePromise(
                $http.get(godModeConfig.baseApiUrl + "GetPropertyEditors")
            );
        },
        getTemplates: function () {
            return umbRequestHelper.resourcePromise(
                $http.get(godModeConfig.baseApiUrl + "GetTemplates")
            );
        },
        getMedia: function () {
            return umbRequestHelper.resourcePromise(
                $http.get(godModeConfig.baseApiUrl + "GetMedia")
            );
        },
        getSurfaceControllers: function () {
            return umbRequestHelper.resourcePromise(
                $http.get(godModeConfig.baseApiUrl + "GetSurfaceControllers")
            );
        },
        getApiControllers: function () {
            return umbRequestHelper.resourcePromise(
                $http.get(godModeConfig.baseApiUrl + "GetApiControllers")
            );
        },
        getRenderMvcControllers: function () {
            return umbRequestHelper.resourcePromise(
                $http.get(godModeConfig.baseApiUrl + "GetRenderMvcControllers")
            );
        },
        getPublishedContentModels: function () {
            return umbRequestHelper.resourcePromise(
                $http.get(godModeConfig.baseApiUrl + "GetPublishedContentModels")
            );
        },
        getPropertyValueConveters: function () {
            return umbRequestHelper.resourcePromise(
                $http.get(godModeConfig.baseApiUrl + "GetPropertyValueConverters")
            );
        },
        getEnvironmentDiagnostics: function () {
            return umbRequestHelper.resourcePromise(
                $http.get(godModeConfig.baseApiUrl + "GetEnvironmentDiagnostics")
            );
        },
        getTriStateOptions: function () {
            return [
                { label: 'Any', value: null },
                { label: 'Yes', value: true },
                { label: 'No', value: false },
            ];
        }
    }
});