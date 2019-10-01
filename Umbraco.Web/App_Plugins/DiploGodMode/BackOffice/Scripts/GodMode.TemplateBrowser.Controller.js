﻿(function () {
    'use strict';
    angular.module("umbraco").controller("GodMode.TemplateBrowser.Controller",
        function ($routeParams, navigationService, godModeResources, godModeConfig, editorService) {

            var vm = this;
            vm.templates = [];

            navigationService.syncTree({ tree: $routeParams.tree, path: [-1, $routeParams.method], forceReload: false });

            vm.config = godModeConfig.config;
            vm.search = {};


            vm.init = function () {
                var openTemplates = vm.templates.filter(function (t) { return t.IsOpen; }).map(function (t) { return t.Id; });
                vm.isLoading = true;

                godModeResources.getTemplates().then(function (data) {
                    vm.templates = data;

                    if (openTemplates) {
                        vm.templates.map(function (t) { if (openTemplates.indexOf(t.Id) > -1) t.IsOpen = true; });
                    }
                    vm.partials = data.map(function (p) { return p.Partials; }).reduce(function (a, b) { return a.concat(b); });
                    vm.masters = data.filter(function (t) { return t.IsMaster; });
                    vm.isLoading = false;
                });
            };

            vm.init();

            vm.filterTemplates = function (temp) {
                if (vm.search.partial) {
                    if (!temp.Partials.some(function (elem) {
                        return elem.Name === vm.search.partial.Name;
                    })) return;
                }

                if (vm.search.master) {
                    if (!temp.Parents.some(function (elem) {
                        return elem.Alias === vm.search.master.Alias;
                    })) return;
                }

                if (vm.search.template) {
                    if (temp.Name.toLowerCase().indexOf(vm.search.template.toLowerCase()) === -1 &&
                        temp.Alias.toLowerCase().indexOf(vm.search.template.toLowerCase()) === -1 &&
                        temp.Udi.toLowerCase().indexOf(vm.search.template.toLowerCase()) === -1) {
                        return;
                    }
                }

                return temp;
            };

            vm.openTemplate = function (templateId) {
                const editor = {
                    id: templateId,
                    submit: function () {
                        vm.init();
                        editorService.close();
                    },
                    close: function () {
                        editorService.close();
                    }
                };
                editorService.templateEditor(editor);
            };

            vm.openPartial = function (path) {
                const editor = {
                    view: "views/partialViews/edit.html",
                    id: path,
                    submit: function () {
                        vm.init();
                        editorService.close();
                    },
                    close: function () {
                        editorService.close();
                    }
                };
                editorService.open(editor);
            };
        });
})();