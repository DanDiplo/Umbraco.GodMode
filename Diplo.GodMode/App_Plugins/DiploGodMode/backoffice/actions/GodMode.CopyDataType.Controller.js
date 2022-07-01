(function () {
    'use strict';

    // Controller for copying a datatype. Yeah, a bit hacky, but works :)

    angular.module("umbraco")
        .controller("GodMode.CopyDataType.Controller",
            function ($routeParams, navigationService, appState, godModeResources, notificationsService) {
                var vm = this;
                vm.toggleShow = false;
                vm.currentNode = appState.getMenuState("currentNode");

                vm.copy = function () {

                    const id = parseInt(vm.currentNode.id);

                    if (id != -1) {

                        godModeResources.copyDataType(id).then(function (data) {

                            if (data.ResponseType == "Success") {
                                notificationsService.success(data.Message);

                                setTimeout(function () {
                                    navigationService.syncTree({ tree: $routeParams.tree, path: [-1, $routeParams.method], forceReload: false });
                                }, 500)
                            } else {
                                notificationsService.error(data.Message);
                            }
                        });
                    }
                    else {
                        notificationsService.error("Not a valid ID to copy");
                    }

                    navigationService.hideDialog();
                };

                vm.cancel = function () {
                    navigationService.hideDialog();
                };

            });
})();