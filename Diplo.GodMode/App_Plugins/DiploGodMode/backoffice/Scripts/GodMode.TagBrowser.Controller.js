(function () {
    'use strict';
    angular.module("umbraco").controller("GodMode.TagBrowser.Controller",
        function ($routeParams, navigationService, godModeResources, editorService) {

            const vm = this;
            vm.isLoading = true;
            vm.search = {};
            vm.tags = [];

            navigationService.syncTree({ tree: $routeParams.tree, path: [-1, $routeParams.method], forceReload: false });

            let fetchContent = function () {
                vm.isLoading = true;
                godModeResources.getTagMapping().then(function (data) {
                    vm.tags = data;
                    vm.isLoading = false;
                });
            };

            vm.tagFilter = function (t) {

                if (vm.search.tagName) {
                    if (t.Tag.Text.toLowerCase().indexOf(vm.search.tagName.toLowerCase()) === -1) {
                        return;
                    }
                }

                if (vm.search.tagGroup) {
                    if (t.Tag.Group.toLowerCase().indexOf(vm.search.tagGroup.toLowerCase()) === -1) {
                        return;
                    }
                }

                if (vm.search.tagContent && t.Content != null) {

                    var mapped = t.Content.map(i => i.Name.toLowerCase());

                    if (mapped.findIndex(element => element.includes(vm.search.tagContent.toLowerCase()))) {
                        return;
                    }
                }

                return t;
            };

            vm.openContent = function (contentId) {
                const editor = {
                    id: contentId,
                    submit: function () {
                        vm.fetchContent();
                        editorService.close();
                    },
                    close: function () {
                        editorService.close();
                    }
                };
                editorService.contentEditor(editor);
            };

            vm.openMedia = function (mediaId) {
                const editor = {
                    id: mediaId,
                    submit: function () {
                        vm.fetchContent();
                        editorService.close();
                    },
                    close: function () {
                        editorService.close();
                    }
                };
                editorService.mediaEditor(editor);
            };

            fetchContent();

        });
})();