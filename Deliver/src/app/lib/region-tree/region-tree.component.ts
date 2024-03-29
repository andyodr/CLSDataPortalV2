import { SelectionModel } from "@angular/cdk/collections"
import { FlatTreeControl } from "@angular/cdk/tree"
import { Component, EventEmitter, Input, Output } from "@angular/core"
import { MatTreeFlatDataSource, MatTreeFlattener, MatTreeModule } from "@angular/material/tree"
import { RegionFilter, RegionFlatNode } from "src/app/_services/hierarchy.service"
import { MatIconModule } from "@angular/material/icon"
import { MatCheckboxModule } from "@angular/material/checkbox"
import { MatButtonModule } from "@angular/material/button"

@Component({
    selector: "app-region-tree",
    templateUrl: "./region-tree.component.html",
    styleUrls: ["./region-tree.component.scss"],
    standalone: true,
    imports: [MatTreeModule, MatButtonModule, MatCheckboxModule, MatIconModule]
})
export class RegionTreeComponent {
    @Input()
    get hierarchy(): RegionFilter[] {
        return this._hierarchy
    }
    set hierarchy(value: RegionFilter[]) {
        this.treeData.data = this._hierarchy = value
        if (this.selectedRegions && this.checklistSelection) {
            if (Array.isArray(this.selectedRegions)) {
                this.setInitialSelections(value, this.selectedRegions)
            }
            else {
                this.setInitialSelection(value, this.selectedRegions)
            }
        }
    }

    private _hierarchy!: RegionFilter[]

    @Input()
    get selectedRegions() {
        if (this.multiple) {
            if (!this.checklistSelection) {
                return []
            }

            return this.checklistSelection.selected
                .map(fn => this.flatNodeMap.get(fn))
                .filter((rf): rf is RegionFilter => !!rf)
                .map(rf => rf.id)
        }
        else if ((this.checklistSelection?.selected.length ?? 0) === 0) {
            return null
        }
        else {
            return this.flatNodeMap.get(this.checklistSelection!.selected[0])?.id ?? -1
        }
    }
    set selectedRegions(value: number | number[] | null) {
        if (Array.isArray(value)) {
            if (!this.checklistSelection) {
                this.checklistSelection = new SelectionModel<RegionFlatNode>(true)
            }

            if (this.hierarchy) {
                this.setInitialSelections(this.hierarchy, value)
            }
        }
        else {
            if (this.hierarchy) {
                if (!this.checklistSelection) {
                    this.checklistSelection = new SelectionModel<RegionFlatNode>(false)
                }

                this.setInitialSelection(this.hierarchy, value)
            }
        }
    }

    @Output() selectedRegionsChange = new EventEmitter<number | number[] | null>()

    ancestorPath: string[] = ["?"]
    descendantPath: string[] = ["?"]
    get multiple() { return this.checklistSelection?.isMultipleSelection() ?? false }
    checklistSelection?: SelectionModel<RegionFlatNode>
    flatNodeMap = new Map<RegionFlatNode, RegionFilter>()
    nestedNodeMap = new Map<RegionFilter, RegionFlatNode>()
    treeControl: FlatTreeControl<RegionFlatNode>
    treeFlattener!: MatTreeFlattener<RegionFilter, RegionFlatNode>
    treeData!: MatTreeFlatDataSource<RegionFilter, RegionFlatNode>
    constructor() {
        this.treeFlattener = new MatTreeFlattener(
            this.transformer,
            this.getLevel,
            this.isExpandable,
            this.getChildren
        )
        this.treeControl = new FlatTreeControl<RegionFlatNode>(this.getLevel, this.isExpandable)
        this.treeData = new MatTreeFlatDataSource(this.treeControl, this.treeFlattener)
        this.treeData.data = []
    }

    createHierarchyMap(hierarchy: RegionFilter[]): Map<number, RegionFilter> {
        let n = 0
        let fh = [hierarchy[0]]  // flattened version of hierarchy
        while (n < fh.length) {
            if (Array.isArray(fh[n].sub) && fh[n].sub.length > 0) {
                fh = fh.concat(...fh[n].sub)
            }

            n++
        }

        return new Map<number, RegionFilter>(fh.map(rf => [rf.id, rf]))
    }

    /** Before calling, checklistSelection must be created */
    setInitialSelections(hierarchy: RegionFilter[], selected: number[]): void {
        if (selected.length > 0) {
            let m = this.createHierarchyMap(hierarchy)
            let initiallySelectedRegions: RegionFlatNode[] = selected
                .map(id => m.get(id)).filter((rf): rf is RegionFilter => !!rf)
                .map(rf => this.nestedNodeMap.get(rf)).filter((fn): fn is RegionFlatNode => !!fn)
            this.checklistSelection!.select(...initiallySelectedRegions)
            this.treeControl.expand(this.treeControl.dataNodes[0])
        }
    }

    /** Before calling, checklistSelection must be created */
    setInitialSelection(hierarchy: RegionFilter[], selected: number | null): void {
        if (selected == null) {
            this.checklistSelection!.clear()
            this.treeControl.collapseAll()
        }
        else {
            // expand the parent nodes to reveal selected parent
            let r = this.createHierarchyMap(hierarchy).get(selected)
            if (r) {
                let node = this.nestedNodeMap.get(r)
                if (!node) return
                // updates DOM after parent change detection, so it must be delayed
                Promise.resolve().then(() => this.setPaths(node!))
                let p: RegionFlatNode | undefined = node
                do {
                    this.treeControl.expand(p)
                    p = this.getParentNode(p!)
                } while (p)

                this.checklistSelection!.select(node)
            }
        }
    }

    /** Set the region path hierarchy from root to selected node */
    setPaths(selectedNode: RegionFlatNode) {
        const path: RegionFlatNode[] = []
        let parent: RegionFlatNode | undefined = selectedNode
        while (parent) {
            path.unshift(parent)
            parent = this.getParentNode(path[0])
        }

        this.ancestorPath = path.map(n => this.flatNodeMap.get(n!)?.hierarchy ?? "?")
        let _ = path.splice(0, Infinity, selectedNode)
        this.descendantPath = path
            .map(n => ([selectedNode, ...(this.flatNodeMap.get(n!)?.sub ?? [])])
            .map(n => n.hierarchy)).flat()
   }

    getLevel = (node: RegionFlatNode): number => node.level
    isExpandable = (node: RegionFlatNode): boolean => node.expandable
    getChildren = (node: RegionFilter): RegionFilter[] => node.sub
    hasChild = (_: number, _nodeData: RegionFlatNode): boolean => _nodeData.expandable
    hasNoContent = (_: number, _nodeData: RegionFlatNode): boolean => _nodeData.hierarchy === ""
    transformer = (node: RegionFilter, level: number): RegionFlatNode => {
        const prevNode = this.nestedNodeMap.get(node)
        const flatNode = prevNode?.hierarchy === node.hierarchy ? prevNode : new RegionFlatNode()
        flatNode.hierarchy = node.hierarchy
        flatNode.level = level
        flatNode.expandable = !!node.sub?.length
        this.flatNodeMap.set(flatNode, node)
        this.nestedNodeMap.set(node, flatNode)
        return flatNode
    }

    /** supports template [checked] binding */
    descendantsAllSelected(node: RegionFlatNode): boolean {
        const descendants = this.treeControl.getDescendants(node)
        const descAllSelected =
            descendants.length > 0 &&
            descendants.every(child => {
                return this.checklistSelection!.isSelected(child)
            })
        return descAllSelected && this.multiple || this.checklistSelection!.isSelected(node)
    }

    /** supports template [indeterminate] binding */
    descendantsPartiallySelected(node: RegionFlatNode): boolean {
        const descendants = this.treeControl.getDescendants(node)
        const result = descendants.some(child => this.checklistSelection!.isSelected(child))
        return result && !this.descendantsAllSelected(node)
    }

    getParentNode(node: RegionFlatNode): RegionFlatNode | undefined {
        const currentLevel = this.getLevel(node)
        if (currentLevel < 1) {
            return
        }

        const startIndex = this.treeControl.dataNodes.indexOf(node) - 1
        for (let i = startIndex; i >= 0; i--) {
            const currentNode = this.treeControl.dataNodes[i]

            if (this.getLevel(currentNode) < currentLevel) {
                return currentNode
            }
        }

        return
    }

    updateRootNodeSelection(node: RegionFlatNode): void {
        if (!this.checklistSelection) return
        const nodeSelected = this.checklistSelection.isSelected(node)
        const descendants = this.treeControl.getDescendants(node)
        const descAllSelected =
            descendants.length > 0 &&
            descendants.every(child => {
                return this.checklistSelection!.isSelected(child)
            })
        if (nodeSelected && !descAllSelected) {
            this.checklistSelection.deselect(node)
            this.removeIds(node)
        }
        else if (!nodeSelected && descAllSelected) {
            this.checklistSelection.select(node)
            this.addIds(node)
        }
    }

    checkAllParentsSelection(node: RegionFlatNode): void {
        let parent: RegionFlatNode | undefined = this.getParentNode(node)
        while (parent) {
            this.updateRootNodeSelection(parent)
            parent = this.getParentNode(parent)
        }
    }

    /** Called by template */
    regionSelectionToggle(node: RegionFlatNode): void {
        if (!this.checklistSelection) return
        this.checklistSelection.toggle(node)
        const descendants = this.treeControl.getDescendants(node)
        if (this.checklistSelection.isSelected(node)) {
            if (this.multiple) {
                this.checklistSelection.select(...descendants)
            }

            this.addIds(...this.checklistSelection.selected)
        }
        else {
            this.removeIds(node, ...descendants)
            if (this.multiple) {
                this.checklistSelection.deselect(...descendants)
            }
        }

        // Force update for the parent
        descendants.forEach(child => this.checklistSelection!.isSelected(child))
        if (this.multiple) {
            this.checkAllParentsSelection(node)
        }
    }

    addIds(...nodes: RegionFlatNode[]) {
        for (let node of nodes) {
            let rh = this.flatNodeMap.get(node)
            if (rh !== undefined) {
                if (!this.multiple) {
                    this.setPaths(node)
                }
            }
        }

        this.selectedRegionsChange.emit(this.selectedRegions)
    }

    removeIds(...nodes: RegionFlatNode[]) {
        this.selectedRegionsChange.emit(this.selectedRegions)
    }

    reset() {
        this.checklistSelection?.clear()
    }
}
