import { Directive, Output, EventEmitter, HostBinding, HostListener } from "@angular/core"

@Directive({ selector: "[fileUpload]" })
export class UploadDirective {
    @Output() onFileDropped: EventEmitter<FileList> = new EventEmitter()
    @HostBinding("style.background-color") public background = "#fff"
    @HostBinding("style.opacity") public opacity = "1"

    @HostListener("dragover", ["$event"]) onDragOver(event: DragEvent) {
        event.preventDefault()
        event.stopPropagation()
        this.background = "#9ecbec"
        this.opacity = ".8"
    }

    @HostListener("dragleave", ["$event"]) public onDragLeave(event: DragEvent) {
        event.preventDefault()
        event.stopPropagation()
        this.background = "#fff"
        this.opacity = "1"
    }

    @HostListener("drop", ["$event"]) public onDrop(event: DragEvent) {
        event.preventDefault()
        event.stopPropagation()
        this.background = "#f5fcff"
        this.opacity = "1"
        let files = event.dataTransfer?.files
        if (files != null && files.length > 0) {
            this.onFileDropped.emit(files)
        }
    }
}
