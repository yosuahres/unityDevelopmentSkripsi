//homeview.swift
//mark 1 november
import SwiftUI
import RealityKit
import UnityFramework 
import PolySpatialRealityKit
import UIKit 

struct HomeView: View {
    
    @State private var models: [CaseGroup] = []
    @State private var selection: String? = nil
    @State private var searchText: String = ""
    @ObservedObject var appState: AppState   

    init(appState: AppState = AppState.shared) {
        _appState = ObservedObject(wrappedValue: appState)
    }
    
    var filteredCaseGroups: [CaseGroup] {
        if searchText.isEmpty {
            return models
        } else {
            return models.filter { group in
                group.name.localizedCaseInsensitiveContains(searchText)
                || group.description.localizedCaseInsensitiveContains(searchText)
                || group.usdzModelNames.contains { $0.localizedCaseInsensitiveContains(searchText) }
            }
        }
    }

    var body: some View {
        NavigationSplitView {
            List(filteredCaseGroups, id: \.primaryModel, selection: $selection) { group in
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
                        Text(group.name)
                            .lineLimit(1)
                        if !group.description.isEmpty {
                            Text(group.description)
                                .font(.subheadline)
                                .foregroundColor(.secondary)
                                .lineLimit(2)
                        }
                        Text(group.primaryModel)
                            .font(.caption)
                            .foregroundColor(.secondary)
                    }
                }
            }
            .navigationTitle("Session")
            .onAppear(perform: loadModelList)
            .searchable(text: $searchText, prompt: "Search groups")
            .onChange(of: selection) { newSelection in
                appState.selectedModel = newSelection
            }
            
        } detail: {
            VStack {
                if let modelName = selection {
                    if let resourceRoot = Bundle.main.resourceURL,
                       let url = URL(string: "Data/Raw/\(modelName)", relativeTo: resourceRoot) {
                        Model3D(url: url) { model in
                            model
                                .resizable()
                                .aspectRatio(contentMode: .fit)
                                .scaleEffect(0.5) 
                                .offset(y: -50)  
                        } placeholder: {
                            ProgressView("Loading \(modelName)...")
                        }
                        
                    } else {
                        Text("Error: Could not find or access model file: \(modelName)")
                            .foregroundColor(.red)
                            .padding()
                    }
                    
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
                        Button("Go to Immersive Space") {
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
        var groups = DummyFragmentData.caseGroups

        if let urls = Bundle.main.urls(forResourcesWithExtension: "usdz", subdirectory: "Data/Raw") {
            let filenames = urls.map { $0.lastPathComponent }
            for fname in filenames {
                let alreadyIncluded = groups.contains { $0.usdzModelNames.contains(fname) }
                if !alreadyIncluded {
                    groups.append(CaseGroup(usdzModelNames: [fname], name: fname, description: ""))
                }
            }
        }

        self.models = groups.sorted { $0.name.localizedCaseInsensitiveCompare($1.name) == .orderedAscending }
    }
}