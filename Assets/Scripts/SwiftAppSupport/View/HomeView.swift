//homeview.swift
import SwiftUI
import RealityKit
import UnityFramework
import PolySpatialRealityKit
import UIKit 

struct HomeView: View {
    @State private var models: [LoadedCaseGroup] = []
    @State private var selection: UUID? = nil
    @State private var searchText: String = ""

    var appState: AppState   

    init(appState: AppState = AppState.shared) {
        self.appState = appState
    }
    
    var selectedLoadedGroup: LoadedCaseGroup? {
        models.first { $0.id == selection }
    }
    
    var primaryUsdzUrl: URL? {
        selectedLoadedGroup?.usdzURLs.first
    }
    
    var primaryModelName: String? {
        selectedLoadedGroup?.group.usdzModelNames.first
    }
    
    var filteredCaseGroups: [LoadedCaseGroup] {
        if searchText.isEmpty {
            return models
        } else {
            return models.filter { loadedGroup in
                
                loadedGroup.group.name.localizedCaseInsensitiveContains(searchText)
                || loadedGroup.group.description.localizedCaseInsensitiveContains(searchText)
                || loadedGroup.group.usdzModelNames.contains { $0.localizedCaseInsensitiveContains(searchText) }
            }
        }
    }

    var body: some View {
        @Bindable var state = appState
        
        NavigationSplitView {
            
            List(filteredCaseGroups, selection: $selection) { loadedGroup in
                HStack(spacing: 8) {
                    if let image = loadImageFromDataRaw(named: "glyph") { 
                        image
                            .resizable()
                            .frame(width: 50, height: 50)
                    } else {
                        EmptyView() 
                            .frame(width: 50, height: 50)
                    }
                    
                    VStack(alignment: .leading) {
                        Text(loadedGroup.group.name)
                            .lineLimit(1)
                        if !loadedGroup.group.description.isEmpty {
                            Text(loadedGroup.group.description)
                                .font(.subheadline)
                                .foregroundColor(.secondary)
                                .lineLimit(2)
                        }
                        
                        Text(loadedGroup.group.usdzModelNames.first ?? "N/A")
                            .font(.caption)
                            .foregroundColor(.secondary)
                    }
                }
            }
            .navigationTitle("Session")
            .onAppear(perform: loadModelList)
            .searchable(text: $searchText, prompt: "Search groups")
            .onChange(of: selection) { _ in
                state.selectedModel = primaryModelName
            }
            
        } detail: {
            VStack {
                
                if let url = primaryUsdzUrl,
                   let modelName = primaryModelName {
                    
                    Model3D(url: url) { model in
                        model
                            .resizable()
                            .aspectRatio(contentMode: .fit)
                            .scaleEffect(0.4) 
                            .offset(y: -50)  
                    } placeholder: {
                        ProgressView("Loading \(modelName)...")
                    }
                    
                } else if selection != nil {
                    
                    Text("Error: Could not find model URL for selected group.")
                        .foregroundColor(.red)
                        .padding()
                } else {
                    Text("No Object Selected")
                    .foregroundColor(.secondary)
                }
            }
        }

        .frame(minWidth: 800, minHeight: 300)

        .toolbar {
            ToolbarItem(placement: .bottomOrnament) {
                VStack {
                    if selection != nil {
                        Button("Continue to Configuration") {
                            CallCSharpCallback("TriggerConfigurationScene")
                        }
                        .disabled(selection == nil)
                    }
                }
            }
        }
    }
    
    func loadImageFromDataRaw(named assetName: String) -> Image? {
        guard let imagesetPath = Bundle.main.path(forResource: assetName, ofType: "imageset", inDirectory: "Data/Raw") else {
            return nil
        }
        
        guard let imagesetBundle = Bundle(path: imagesetPath) else {
            return nil
        }

        guard let uiImage = UIImage(named: assetName, in: imagesetBundle, compatibleWith: nil) else {
            return nil
        }
        return Image(uiImage: uiImage)
    }
    
    
    func loadModelList() {
        var groups: [CaseGroup] = []

        
        var baseGroups = DummyFragmentData.caseGroups

        
        if let urls = Bundle.main.urls(forResourcesWithExtension: "usdz", subdirectory: "Data/Raw") {
            let filenames = urls.map { $0.lastPathComponent }
            for fname in filenames {
                let alreadyIncluded = baseGroups.contains { $0.usdzModelNames.contains(fname) }
                if !alreadyIncluded {
                    
                    let newGroup = CaseGroup(id: UUID(), usdzModelNames: [fname], name: fname, description: "")
                    baseGroups.append(newGroup)
                }
            }
        }
        
        
        groups = baseGroups.filter { !$0.usdzModelNames.isEmpty }
        let sortedGroups = groups.sorted { $0.name.localizedCaseInsensitiveCompare($1.name) == .orderedAscending }
        
        
        let resourceRoot = Bundle.main.resourceURL
        
        let loadedGroups: [LoadedCaseGroup] = sortedGroups.map { group in
            
            
            let urls: [URL] = group.usdzModelNames.compactMap { modelName in
                guard let root = resourceRoot else { return nil }
                return URL(string: "Data/Raw/\(modelName)", relativeTo: root)
            }
            
            
            let entities: [Entity?] = Array(repeating: nil, count: urls.count)

            return LoadedCaseGroup(
                id: UUID(), 
                group: group,
                usdzURLs: urls,
                usdzEntities: entities
            )
        }
        
        self.models = loadedGroups
    }
}