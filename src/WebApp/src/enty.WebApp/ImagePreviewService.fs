namespace enty.WebApp


module ImagePreviewServiceImpl =

    open enty.ImagePreviewService.Client.Fable
    
    let imagePreview: IImagePreviewUrlProvider =
        upcast BaseAddressImagePreviewUrlProvider("/preview")
