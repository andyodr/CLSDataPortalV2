import { Injectable, Directive, HostListener, Input } from "@angular/core"
import { Location } from "@angular/common"
import { Router, NavigationEnd, ActivatedRoute } from "@angular/router"

@Injectable({ providedIn: "root" })
export class NavigationService {
    private history: string[] = []

    constructor(private router: Router, private location: Location) {
        this.router.events.subscribe((event) => {
            if (event instanceof NavigationEnd) {
                this.history.push(event.urlAfterRedirects)
            }
        });
    }

    back(commands?: any[]): void {
        this.history.pop()
        if (this.history.length > 0 || commands == null) {
            this.location.back()
        }
        else {
            this.router.navigate(commands)
        }
    }
}

@Directive({
    selector: "[navigateBack]",
})
export class NavigateBackDirective {
    constructor(private navigation: NavigationService, private route: ActivatedRoute) { }

    @Input() navigateBack?: string
    @HostListener("click")
    onClick(): void {
        if (this.navigateBack == null) {
            this.navigation.back()
        } else {
            this.navigation.back([this.navigateBack])
        }
    }
}
