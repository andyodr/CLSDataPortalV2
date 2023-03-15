import { SelectionModel } from "@angular/cdk/collections"
import { FlatTreeControl } from "@angular/cdk/tree"
import { Component, EventEmitter, Input, Output } from "@angular/core"
import { MatTreeFlatDataSource, MatTreeFlattener } from "@angular/material/tree"
import { RegionFilter, RegionFlatNode } from "../../_models/regionhierarchy"

@Component({
    selector: "app-region-tree",
    templateUrl: "./region-tree.component.html",
    styleUrls: ["./region-tree.component.scss"]
})
export class RegionTreeComponent {
    @Input()
    get hierarchy(): RegionFilter[] {
        return this._hierarchy
    }

    set hierarchy(value: RegionFilter[]) {
        this.treeData.data = this._hierarchy = value
        this.setInitialSelections(value, this.selectedRegions)
    }

    private _hierarchy!: RegionFilter[]

    @Input()
    get selectedRegions() {
        if (typeof this._selectedRegions === "number") {
            return this._selectedRegions
        }
        else if (this._selectedRegions == null) {
            return null
        }
        else {
            return [...this._selectedRegions.keys()]
        }
    }

    set selectedRegions(value: number | number[] | null) {
        if (Array.isArray(value)) {
            this._selectedRegions = new Set<number>(value)
            if (this.hierarchy) {
                this.setInitialSelections(this.hierarchy, value)
            }
        }
        else {
            this._selectedRegions = value
            if (this.hierarchy) {
                if (this.checklistSelection.isMultipleSelection()) {
                    this.checklistSelection = new SelectionModel<RegionFlatNode>(false)
                }

                this.setInitialSelections(this.hierarchy, value)
            }
        }
    }

    private _selectedRegions: number | Set<number> | null = null
    @Output() selectedRegionsChange = new EventEmitter<number | number[] | null>()

    checklistSelection = new SelectionModel<RegionFlatNode>(true)
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

    setInitialSelections(hierarchy: RegionFilter[], value: number | number[] | null): void {
        if (Array.isArray(value)) {
            if (value.length > 0) {
                let m = this.createHierarchyMap(hierarchy)
                let initiallySelectedRegions: RegionFlatNode[] = value
                    .map(id => m.get(id)).filter((rf): rf is RegionFilter => !!rf)
                    .map(rf => this.nestedNodeMap.get(rf)).filter((fn): fn is RegionFlatNode => !!fn)
                this.checklistSelection.select(...initiallySelectedRegions)
            }
        }
        else if (value == null) {
            this.checklistSelection.clear()
            this.treeControl.collapseAll()
        }
        else {
            // expand the parent nodes to reveal selected parent
            let r = this.createHierarchyMap(hierarchy).get(value)
            if (r) {
                let node = this.nestedNodeMap.get(r)
                if (node) {
                    let p: RegionFlatNode | undefined = node
                    do {
                        p = this.getParentNode(p!)
                        if (p) {
                            this.treeControl.expand(p)
                        }
                    } while (p)

                    this.checklistSelection.select(node)
                }
            }
        }
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

    descendantsAllSelected(node: RegionFlatNode): boolean {
        if (this.checklistSelection.isMultipleSelection()) {
            const descendants = this.treeControl.getDescendants(node)
            const descAllSelected =
                descendants.length > 0 &&
                descendants.every(child => {
                    return this.checklistSelection.isSelected(child)
                })
            return descAllSelected
        }
        else {
            return this.checklistSelection.isSelected(node)
        }
    }

    descendantsPartiallySelected(node: RegionFlatNode): boolean {
        if (this.checklistSelection.isMultipleSelection()) {
            const descendants = this.treeControl.getDescendants(node)
            const result = descendants.some(child => this.checklistSelection.isSelected(child))
            return result && !this.descendantsAllSelected(node)
        }
        else {
            return false
        }
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
        const nodeSelected = this.checklistSelection.isSelected(node)
        const descendants = this.treeControl.getDescendants(node)
        const descAllSelected =
            descendants.length > 0 &&
            descendants.every(child => {
                return this.checklistSelection.isSelected(child)
            })
        if (nodeSelected && !descAllSelected) {
            this.checklistSelection.deselect(node)
            this.removeSelected(node)
        }
        else if (!nodeSelected && descAllSelected) {
            this.checklistSelection.select(node)
            this.addSelected(node)
        }
    }

    checkAllParentsSelection(node: RegionFlatNode): void {
        let parent: RegionFlatNode | undefined = this.getParentNode(node)
        while (parent) {
            this.updateRootNodeSelection(parent)
            parent = this.getParentNode(parent)
        }
    }

    regionSelectionToggle(node: RegionFlatNode): void {
        this.checklistSelection.toggle(node)
        if (this.checklistSelection.isMultipleSelection()) {
            const descendants = this.treeControl.getDescendants(node)
            if (this.checklistSelection.isSelected(node)) {
                this.checklistSelection.select(...descendants)
                this.addSelected(node, ...descendants)
            }
            else {
                this.checklistSelection.deselect(...descendants)
                this.removeSelected(node, ...descendants)
            }

            // Force update for the parent
            descendants.forEach(child => this.checklistSelection.isSelected(child))
        }

        this.checkAllParentsSelection(node)
    }

    addSelected(...nodes: RegionFlatNode[]) {
        for (let node of nodes) {
            let rh = this.flatNodeMap.get(node)
            if (rh !== undefined) {
                if (typeof this._selectedRegions === "number" || this._selectedRegions == null) {
                    this._selectedRegions = rh.id
                }
                else {
                    this._selectedRegions.add(rh.id)
                }
            }
        }

        this.selectedRegionsChange.emit(this.selectedRegions)
    }

    removeSelected(...nodes: RegionFlatNode[]) {
        for (let node of nodes) {
            let rh = this.flatNodeMap.get(node)
            if (rh !== undefined) {
                if (typeof this._selectedRegions === "object" && this._selectedRegions != null) {
                    this._selectedRegions.delete(rh.id)
                }
                else {
                    if (this._selectedRegions === rh.id) {
                        this._selectedRegions = null
                    }
                }
            }
        }

        this.selectedRegionsChange.emit(this.selectedRegions)
    }

    reset() {
        this.checklistSelection.clear()
    }
}
