import { Component, Input } from "@angular/core"

@Component({
    selector: "app-errors",
    template: `
<div class="panel panel-default ng-hide noBorder marB0">
  <div class="panel-body thick-red-border p-2 mat-elevation-z4">
    @if (showContentPage) {
        <p>{{error.heading}}</p>
        @if (error.id != null) {
          <p>{{error.id}}</p>
        }
        @if (error.status != null) {
          <p>{{error.status}}</p>
        }
        <p>{{error.message}}</p>
    }
    @else {
        @if (error.id != null) {
          <p>{{error.id}}</p>
        }
        @if (error.status != null) {
          <p>{{error.status}}</p>
        }
        <p>{{error.message}}</p>
    }
  </div>
</div>`,
    standalone: true,
    imports: []
})
export class ErrorsComponent {
    @Input() error!: { id: any, status: any, message: any, heading: any }
    @Input() showContentPage!: boolean
    @Input() closeError!: () => void
}
