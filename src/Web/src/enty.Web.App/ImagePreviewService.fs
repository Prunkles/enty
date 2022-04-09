namespace enty.Web.App


module ImagePreviewServiceImpl =

    open enty.ImagePreviewService.Client.Fable

    let imagePreview: IImagePreviewUrlProvider =
        upcast BaseAddressImagePreviewUrlProvider("/preview")
