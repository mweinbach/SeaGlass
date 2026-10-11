plugins {
    alias(libs.plugins.android.library)
    `maven-publish`
}

group = "dev.seaglass"
version = "1.0.0"

android {
    namespace = "dev.seaglass.view"
    compileSdk = 36

    defaultConfig {
        minSdk = 26
        consumerProguardFiles("consumer-rules.pro")
    }

    publishing {
        singleVariant("release") {
            withSourcesJar()
        }
    }

    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_17
        targetCompatibility = JavaVersion.VERSION_17
    }

    buildFeatures {
        buildConfig = false
    }
}

kotlin {
    jvmToolchain(17)
}

dependencies {
    api(project(":seaglass-core"))
    implementation(libs.androidx.core.ktx)

    testImplementation(libs.junit)
}

afterEvaluate {
    publishing {
        publications {
            create<MavenPublication>("release") {
                from(components["release"])
                groupId = "dev.seaglass"
                artifactId = "seaglass-view"
                version = "1.0.0"
            }
        }
    }
}
