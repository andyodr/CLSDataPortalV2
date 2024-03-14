import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-errors',
    template: `
<div class="panel panel-default ng-hide noBorder marB0">
  <div class="panel-body padB0 marB0 noBorder">
    @if (showContentPage) {
      <ngb-alert type="danger" (closed)="closeError()">
        <p>{{error.heading}}</p>
        @if (error.id != null) {
          <p>{{error.id}}</p>
        }
        @if (error.status != null) {
          <p>{{error.status}}</p>
        }
        <p>{{error.message}}</p>
      </ngb-alert>
    }

    @if (!showContentPage) {
      <ngb-alert type="danger">
        @if (error.id != null) {
          <p>{{error.id}}</p>
        }
        @if (error.status != null) {
          <p>{{error.status}}</p>
        }
        <p>{{error.message}}</p>
      </ngb-alert>
    }
  </div>
</div>`
})
export class ErrorsComponent {
    @Input() error!: { id: any, status: any, message: any, heading: any }
    @Input() showContentPage!: boolean
    @Input() closeError!: () => void
}
