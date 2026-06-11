import java.lang.Math;
boolean a = false;
int pointX = 300;
int pointY = 300;
float beamAngle = 0;

void setup() {
  size(800, 600);
}

void draw() {
  background(255);

  // Circle/Mirror variables
  int circleDiameter = 600; 
  float radius = circleDiameter / 2.0f;
  float centralPointX = width * 0.75f - radius;
  float centralPointY = height / 2.0f;

  // --- Arc Math ---
  float cutDistanceY = height / 2.0f - height / 3.0f; 
  float ratio = constrain(cutDistanceY / radius, 0, 1); 
  float maxAngle = asin(ratio);

  // Draw the visible mirror arc
  noFill();
  stroke(0);
  strokeWeight(3);
  arc(centralPointX, centralPointY, circleDiameter, circleDiameter, -maxAngle, maxAngle);

  // Interaction Logic
  if (a) {
    if (mousePressed) {
      // Swapped the variables to point exactly away from the mouse
      float rawAngle = atan2(pointY - mouseY, pointX - mouseX);
      beamAngle = radians(round(degrees(rawAngle)));
    }
  } else {
    if (mousePressed) {
      pointX = mouseX;
      pointY = mouseY;
    }
  }

  // --- Ray-Circle Intersection Math ---
  float dx = pointX - centralPointX;
  float dy = pointY - centralPointY;

  float b = 2 * (dx * cos(beamAngle) + dy * sin(beamAngle));
  float c = (dx * dx + dy * dy) - (radius * radius);
  float discriminant = (b * b) - (4 * 1 * c);

  float endX = pointX + 2000 * cos(beamAngle);
  float endY = pointY + 2000 * sin(beamAngle);
  
  boolean hitMirror = false; 

  if (discriminant >= 0) {
    float t1 = (-b + sqrt(discriminant)) / 2.0f;
    float t2 = (-b - sqrt(discriminant)) / 2.0f;

    float validT = -1;
    float minValidT = Float.MAX_VALUE;

    float[] possibleTs = {t1, t2};

    for (int i = 0; i < 2; i++) {
      float currentT = possibleTs[i];

      if (currentT >= 0) { 
        float hitX = pointX + currentT * cos(beamAngle);
        float hitY = pointY + currentT * sin(beamAngle);
        
        float hitAngle = atan2(hitY - centralPointY, hitX - centralPointX);
        
        if (hitAngle >= -maxAngle && hitAngle <= maxAngle) {
          if (currentT < minValidT) {
            minValidT = currentT;
            validT = currentT;
          }
        }
      }
    }

    if (validT >= 0) {
      endX = pointX + validT * cos(beamAngle);
      endY = pointY + validT * sin(beamAngle);
      hitMirror = true; 
    }
  }

  // ==========================================
  // 1. DRAW LINES FIRST 
  // ==========================================
  // Incoming Beam
  stroke(0);
  strokeWeight(2);
  line(pointX, pointY, endX, endY);
  
  if (hitMirror) {
    // Normal Line (Red)
    stroke(255, 0, 0);
    strokeWeight(1); 
    line(endX, endY, centralPointX, centralPointY);
    
    // Create two vectors starting from the hit point
    float v1x = pointX - endX;         
    float v1y = pointY - endY;
    float v2x = centralPointX - endX;  
    float v2y = centralPointY - endY;
    
    // Calculate Angle of Incidence
    float dotProduct = (v1x * v2x) + (v1y * v2y);
    float mag1 = dist(endX, endY, pointX, pointY);
    float mag2 = radius; // The distance to the center is always exactly the radius
    
    float angleRad = acos(dotProduct / (mag1 * mag2));
    float angleDeg = degrees(angleRad);
    
    println("Angle of Incidence: " + nf(angleDeg, 0, 2) + "°");
    
    // --- Vector Reflection Math ---
    // Incident vector direction
    float Ix = cos(beamAngle);
    float Iy = sin(beamAngle);
    
    // Normalize the Normal vector to a length of 1
    float Nx = v2x / radius;
    float Ny = v2y / radius;
    
    // Calculate dot product of Incident and Normal
    float dotProductIN = (Ix * Nx) + (Iy * Ny);
    
    // Calculate the Reflected vector (Rx, Ry)
    float Rx = Ix - 2 * dotProductIN * Nx;
    float Ry = Iy - 2 * dotProductIN * Ny;
    
    // Draw Reflected Beam (Blue)
    stroke(0, 0, 255);
    strokeWeight(2);
    line(endX, endY, endX + 2000 * Rx, endY + 2000 * Ry);
  }

  // ==========================================
  // 2. DRAW DOTS LAST 
  // ==========================================
  noStroke();
  
  // Origin Dot
  if (a) {
    fill(255, 0, 0);
  } else {
    fill(0, 255, 0);
  }
  circle(pointX, pointY, 10);
  
  // Center Dot
  fill(0);
  circle(centralPointX, centralPointY, 10);
}

void keyReleased() {
  if (key == 'a') a = !a;
}
