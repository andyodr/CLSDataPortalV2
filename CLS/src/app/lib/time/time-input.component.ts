import { Component, ElementRef, Inject,
    Input, OnDestroy, Optional, Self, ViewChild } from "@angular/core"
import { FocusMonitor } from "@angular/cdk/a11y"
import { BooleanInput, coerceBooleanProperty, coerceNumberProperty } from "@angular/cdk/coercion"
import { AbstractControl, ControlValueAccessor, FormBuilder,
    FormGroup, NgControl, ValidationErrors, Validators } from "@angular/forms"
import { MAT_FORM_FIELD, MatFormField, MatFormFieldControl } from "@angular/material/form-field"
import { Subject } from "rxjs"

/** Data structure for holding telephone number. */
export class TimeSpan {
    constructor(public hours: number, public minutes: number, public seconds: number) { }
}

@Component({
    selector: "app-time-input",
    templateUrl: "./time-input.component.html",
    styleUrls: ["./time-input.component.scss"],
    providers: [{ provide: MatFormFieldControl, useExisting: TimeInputComponent }],
    host: {
        "[class.time-floating]": "shouldLabelFloat",
        "[id]": "id",
    },
})
export class TimeInputComponent implements MatFormFieldControl<TimeSpan>, ControlValueAccessor, OnDestroy {
    static nextId = 0
    @ViewChild("hours") hoursInput!: HTMLInputElement
    @ViewChild("minutes") minutesInput!: HTMLInputElement
    @ViewChild("seconds") secondsInput!: HTMLInputElement

    parts: FormGroup
    stateChanges = new Subject<void>()
    focused = false
    touched = false
    controlType = "app-time-input"
    id = `app-time-input-${ TimeInputComponent.nextId++ }`
    onChange = (_: any) => { }
    onTouched = () => { }

    get empty() {
        const {
            value: { hours, minutes, seconds },
        } = this.parts

        return !hours && !minutes && !seconds
    }

    get shouldLabelFloat() {
        return this.focused || !this.empty
    }

    @Input("aria-describedby") userAriaDescribedBy!: string

    @Input()
    get placeholder(): string {
        return this._placeholder ?? ""
    }
    set placeholder(value: string) {
        this._placeholder = value
        this.stateChanges.next()
    }
    private _placeholder!: string

    @Input()
    get required(): boolean {
        return this._required
    }
    set required(value: BooleanInput) {
        this._required = coerceBooleanProperty(value)
        this.stateChanges.next()
    }
    private _required = false

    @Input()
    get disabled(): boolean {
        return this._disabled
    }
    set disabled(value: BooleanInput) {
        this._disabled = coerceBooleanProperty(value)
        this._disabled ? this.parts.disable() : this.parts.enable()
        this.stateChanges.next()
    }
    private _disabled = false

    @Input()
    get value(): TimeSpan | null {
        if (this.parts.valid) {
            const { value: { hours, minutes, seconds } } = this.parts
            return new TimeSpan(
                coerceNumberProperty(hours),
                coerceNumberProperty(minutes),
                coerceNumberProperty(seconds))
        }

        return null
    }
    set value(value: TimeSpan | null) {
        const { hours, minutes, seconds } = value || new TimeSpan(0, 0, 0)
        this.parts.setValue({
            hours: (hours + "").padStart(2, "0"),
            minutes: (minutes + "").padStart(2, "0"),
            seconds: (seconds + "").padStart(2, "0")
        })

        this.stateChanges.next()
    }

    get errorState(): boolean {
        return this.parts.invalid && this.touched
    }

    constructor(
        formBuilder: FormBuilder,
        private _focusMonitor: FocusMonitor,
        private _elementRef: ElementRef<HTMLElement>,
        @Optional() @Inject(MAT_FORM_FIELD) public _formField: MatFormField,
        @Optional() @Self() public ngControl: NgControl,
    ) {
        this.parts = formBuilder.group({
            hours: [null, [Validators.maxLength(2)]],
            minutes: [null, [Validators.maxLength(2)]],
            seconds: [null, [Validators.maxLength(2)]]
        }, {
            validators: [(form: FormGroup): ValidationErrors | null => {
                const hours: number = coerceNumberProperty(form.get("hours")?.value)
                const minutes: number = coerceNumberProperty(form.get("minutes")?.value)
                const seconds: number = coerceNumberProperty(form.get("seconds")?.value)
                return (hours > 0 || minutes > 0 || seconds > 0) && hours < 100 && minutes < 60 && seconds < 60
                    ? null : { zeroTimeSpan: true }
            }]
        })

        if (this.ngControl != null) {
            this.ngControl.valueAccessor = this
        }
    }

    ngOnDestroy() {
        this.stateChanges.complete()
        this._focusMonitor.stopMonitoring(this._elementRef)
    }

    onFocusIn(event: FocusEvent) {
        if (!this.focused) {
            this.focused = true
            this.stateChanges.next()
        }
    }

    onFocusOut(event: FocusEvent) {
        if (!this._elementRef.nativeElement.contains(event.relatedTarget as Element)) {
            this.touched = true
            this.focused = false
            this.onTouched()
            this.stateChanges.next()
        }
    }

    autoFocusNext(control: AbstractControl, nextElement?: HTMLInputElement): void {
        if (control.value.length > 1 && !control.errors && nextElement) {
            this._focusMonitor.focusVia(nextElement, "program")
        }
    }

    autoFocusPrev(control: AbstractControl, prevElement: HTMLInputElement): void {
        if (control.value.length < 1) {
            this._focusMonitor.focusVia(prevElement, "program")
        }
    }

    setDescribedByIds(ids: string[]) {
        const controlElement = this._elementRef.nativeElement.querySelector(
            ".time-input-container",
        )!
        controlElement.setAttribute("aria-describedby", ids.join(" "))
    }

    onContainerClick() {
    }

    writeValue(value: TimeSpan | null): void {
        this.value = value
    }

    registerOnChange(fn: any): void {
        this.onChange = fn
    }

    registerOnTouched(fn: any): void {
        this.onTouched = fn
    }

    setDisabledState(isDisabled: boolean): void {
        this.disabled = isDisabled
    }

    _handleInput(control: AbstractControl, nextElement?: HTMLInputElement): void {
        this.autoFocusNext(control, nextElement)
        this.onChange(this.value)
    }
}
